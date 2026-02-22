using Longa.Application.Common.Interfaces;
using Longa.Application.Common.Models;
using Longa.Domain.Entities;
using Longa.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Longa.API;

public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bookings", PostBookingRiderSelectsDriver)
            .WithName("PostBookingRiderSelectsDriver")
            .Produces<BookingResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/bookings/driver-select", PostBookingDriverSelectsRider)
            .WithName("PostBookingDriverSelectsRider")
            .Produces<BookingResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> PostBookingRiderSelectsDriver(
        [FromHeader(Name = UsersEndpoints.IdentifierForVendorHeader)] string? identifierForVendor,
        [FromBody] CreateBookingRequest request,
        IUserRepository userRepo,
        LongaDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifierForVendor))
            return ErrorResults.BadRequest($"Header '{UsersEndpoints.IdentifierForVendorHeader}' is required.");

        var user = await userRepo.GetByIdentifierForVendorAsync(identifierForVendor.Trim(), cancellationToken);
        if (user is null)
            return ErrorResults.NotFound("User not found. Create user first via POST /users.");

        if (request.DepartureAt <= DateTimeOffset.UtcNow)
            return ErrorResults.BadRequest("Departure time must be in the future.");
        if (request.DepartureAt > DateTimeOffset.UtcNow.AddDays(30))
            return ErrorResults.BadRequest("Departure time must be within 30 days from now.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var driverTrip = await db.Trips
                .FromSqlRaw("SELECT * FROM trips WHERE id = {0} FOR UPDATE", request.DriverTripId)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (driverTrip is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ErrorResults.NotFound("Driver trip not found.");
            }
            if (driverTrip.Role != TripRole.Driver)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ErrorResults.BadRequest("Trip is not a driver trip.");
            }
            if (driverTrip.Status != TripStatus.Open)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ErrorResults.Conflict("This trip has already been booked.");
            }

            var riderTrip = new Trip
            {
                UserId = user.Id,
                Role = TripRole.Rider,
                Status = TripStatus.Booked,
                PickupAddress = request.PickupAddress?.Trim() ?? "",
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DestinationAddress = request.DestinationAddress?.Trim() ?? "",
                DestinationLat = request.DestinationLat,
                DestinationLng = request.DestinationLng,
                DepartureAt = request.DepartureAt,
                PriceDisplay = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Trips.Add(riderTrip);
            await db.SaveChangesAsync(cancellationToken);

            driverTrip.Status = TripStatus.Booked;
            driverTrip.UpdatedAt = DateTimeOffset.UtcNow;

            var booking = new Booking
            {
                DriverTripId = driverTrip.Id,
                RiderTripId = riderTrip.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Created($"/bookings/{booking.Id}", new BookingResponse(
                booking.Id,
                booking.DriverTripId,
                booking.RiderTripId,
                booking.CreatedAt
            ));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<IResult> PostBookingDriverSelectsRider(
        [FromHeader(Name = UsersEndpoints.IdentifierForVendorHeader)] string? identifierForVendor,
        [FromBody] CreateBookingDriverSelectRequest request,
        IUserRepository userRepo,
        LongaDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifierForVendor))
            return ErrorResults.BadRequest($"Header '{UsersEndpoints.IdentifierForVendorHeader}' is required.");

        var user = await userRepo.GetByIdentifierForVendorAsync(identifierForVendor.Trim(), cancellationToken);
        if (user is null)
            return ErrorResults.NotFound("User not found. Create user first via POST /users.");

        if (request.DepartureAt <= DateTimeOffset.UtcNow)
            return ErrorResults.BadRequest("Departure time must be in the future.");
        if (request.DepartureAt > DateTimeOffset.UtcNow.AddDays(30))
            return ErrorResults.BadRequest("Departure time must be within 30 days from now.");
        if (string.IsNullOrWhiteSpace(request.PriceDisplay))
            return ErrorResults.BadRequest("Price is required when confirming as driver.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var riderTrip = await db.Trips
                .FromSqlRaw("SELECT * FROM trips WHERE id = {0} FOR UPDATE", request.RiderTripId)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (riderTrip is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ErrorResults.NotFound("Rider trip not found.");
            }
            if (riderTrip.Role != TripRole.Rider)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ErrorResults.BadRequest("Trip is not a rider trip.");
            }
            if (riderTrip.Status != TripStatus.Open)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ErrorResults.Conflict("This rider has already been matched.");
            }

            var driverTrip = new Trip
            {
                UserId = user.Id,
                Role = TripRole.Driver,
                Status = TripStatus.Booked,
                PickupAddress = request.PickupAddress?.Trim() ?? "",
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DestinationAddress = request.DestinationAddress?.Trim() ?? "",
                DestinationLat = request.DestinationLat,
                DestinationLng = request.DestinationLng,
                DepartureAt = request.DepartureAt,
                PriceDisplay = request.PriceDisplay?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Trips.Add(driverTrip);
            await db.SaveChangesAsync(cancellationToken);

            riderTrip.Status = TripStatus.Booked;
            riderTrip.UpdatedAt = DateTimeOffset.UtcNow;

            var booking = new Booking
            {
                DriverTripId = driverTrip.Id,
                RiderTripId = riderTrip.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Created($"/bookings/{booking.Id}", new BookingResponse(
                booking.Id,
                booking.DriverTripId,
                booking.RiderTripId,
                booking.CreatedAt
            ));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
