using Longa.Application.Common.Interfaces;
using Longa.Application.Common.Models;
using Longa.Domain.Entities;
using Longa.Infrastructure.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Longa.API;

public static class TripsEndpoints
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

    public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/trips", PostTrip)
            .WithName("PostTrip")
            .Produces<TripResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/trips", GetTrips)
            .WithName("GetTrips")
            .Produces<IReadOnlyList<TripResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> PostTrip(
        [FromHeader(Name = UsersEndpoints.IdentifierForVendorHeader)] string? identifierForVendor,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKeyStr,
        [FromBody] CreateTripRequest request,
        IUserRepository userRepo,
        ITripRepository tripRepo,
        IIdempotencyRepository idempotencyRepo,
        IMatchingService matchingService,
        IOptions<MatchingOptions> options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifierForVendor))
            return ErrorResults.BadRequest($"Header '{UsersEndpoints.IdentifierForVendorHeader}' is required.");

        var user = await userRepo.GetByIdentifierForVendorAsync(identifierForVendor.Trim(), cancellationToken);
        if (user is null)
            return ErrorResults.NotFound("User not found. Create user first via POST /users.");

        if (!TryParseRole(request.Role, out var role))
            return ErrorResults.BadRequest("Role must be 'driver' or 'rider'.");
        if (role == TripRole.Driver && string.IsNullOrWhiteSpace(request.PriceDisplay))
            return ErrorResults.BadRequest("PriceDisplay is required for driver trips.");
        if (request.DepartureAt <= DateTimeOffset.UtcNow)
            return ErrorResults.BadRequest("Departure time must be in the future.");
        if (request.DepartureAt > DateTimeOffset.UtcNow.AddDays(30))
            return ErrorResults.BadRequest("Departure time must be within 30 days from now.");

        Guid? idempotencyKey = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKeyStr) && Guid.TryParse(idempotencyKeyStr, out var parsed))
            idempotencyKey = parsed;

        if (idempotencyKey.HasValue)
        {
            var (exists, tripId) = await idempotencyRepo.TryGetExistingAsync(
                idempotencyKey.Value, user.Id, IdempotencyTtl, cancellationToken);
            if (exists && tripId.HasValue)
            {
                var existingTrip = await tripRepo.GetByIdAsync(tripId.Value, cancellationToken);
                if (existingTrip is not null)
                    return Results.Created($"/trips/{existingTrip.Id}", ToTripResponse(existingTrip));
            }
        }

        var trip = new Trip
        {
            UserId = user.Id,
            Role = role,
            Status = TripStatus.Open,
            PickupAddress = request.PickupAddress?.Trim() ?? "",
            PickupLat = request.PickupLat,
            PickupLng = request.PickupLng,
            DestinationAddress = request.DestinationAddress?.Trim() ?? "",
            DestinationLat = request.DestinationLat,
            DestinationLng = request.DestinationLng,
            DepartureAt = request.DepartureAt,
            PriceDisplay = role == TripRole.Driver ? request.PriceDisplay?.Trim() : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        trip = await tripRepo.CreateAsync(trip, cancellationToken);

        if (idempotencyKey.HasValue)
            await idempotencyRepo.StoreAsync(idempotencyKey.Value, user.Id, trip.Id, cancellationToken);

        if (trip.Status == TripStatus.Open)
        {
            var booking = role == TripRole.Driver
                ? await matchingService.TryMatchDriverTripAsync(trip, cancellationToken)
                : await matchingService.TryMatchRiderTripAsync(trip, cancellationToken);

            if (booking is not null)
            {
                var reloaded = await tripRepo.GetByIdAsync(trip.Id, cancellationToken);
                if (reloaded is not null)
                    trip = reloaded;
            }
        }

        return Results.Created($"/trips/{trip.Id}", ToTripResponse(trip));
    }

    private static async Task<IResult> GetTrips(
        [FromQuery] string? role,
        [FromQuery] decimal? pickupLat,
        [FromQuery] decimal? pickupLng,
        [FromQuery] decimal? destinationLat,
        [FromQuery] decimal? destinationLng,
        [FromQuery] DateTimeOffset? departureAt,
        ITripRepository tripRepo,
        IOptions<MatchingOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryParseRole(role ?? "", out var roleParsed))
            return ErrorResults.BadRequest("Query 'role' must be 'driver' or 'rider'.");
        if (pickupLat is null || pickupLng is null || destinationLat is null || destinationLng is null || departureAt is null)
            return ErrorResults.BadRequest("pickupLat, pickupLng, destinationLat, destinationLng, and departureAt are required.");
        if (departureAt.Value <= DateTimeOffset.UtcNow)
            return ErrorResults.BadRequest("Departure time must be in the future.");
        if (departureAt.Value > DateTimeOffset.UtcNow.AddDays(30))
            return ErrorResults.BadRequest("Departure time must be within 30 days from now.");

        var cfg = options.Value;
        var trips = roleParsed == TripRole.Driver
            ? await tripRepo.GetOpenDriverTripsAsync(
                pickupLat.Value, pickupLng.Value,
                destinationLat.Value, destinationLng.Value,
                departureAt.Value,
                cfg.PickupRadiusKm, cfg.DestinationRadiusKm, cfg.DepartureWindowMinutes,
                cancellationToken)
            : await tripRepo.GetOpenRiderTripsAsync(
                pickupLat.Value, pickupLng.Value,
                destinationLat.Value, destinationLng.Value,
                departureAt.Value,
                cfg.PickupRadiusKm, cfg.DestinationRadiusKm, cfg.DepartureWindowMinutes,
                cancellationToken);

        var response = trips.Select(ToTripResponse).ToList();
        return Results.Ok(response);
    }

    private static bool TryParseRole(string value, out TripRole role)
    {
        role = default;
        if (string.Equals(value, "driver", StringComparison.OrdinalIgnoreCase))
        {
            role = TripRole.Driver;
            return true;
        }
        if (string.Equals(value, "rider", StringComparison.OrdinalIgnoreCase))
        {
            role = TripRole.Rider;
            return true;
        }
        return false;
    }

    private static TripResponse ToTripResponse(Trip t) => new(
        t.Id,
        t.UserId,
        t.Role.ToString().ToLowerInvariant(),
        t.Status.ToString().ToLowerInvariant(),
        t.PickupAddress,
        t.PickupLat,
        t.PickupLng,
        t.DestinationAddress,
        t.DestinationLat,
        t.DestinationLng,
        t.DepartureAt,
        t.PriceDisplay,
        t.CreatedAt
    );

}
