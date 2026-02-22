using Longa.Application.Common.Interfaces;
using Longa.Domain.Entities;
using Longa.Infrastructure.Data;
using Longa.Infrastructure.Options;
using Longa.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Longa.Infrastructure.Repositories;

public class TripRepository(
    LongaDbContext db,
    IOptions<MatchingOptions> options) : ITripRepository
{
    public async Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Trip> CreateAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        db.Trips.Add(trip);
        await db.SaveChangesAsync(cancellationToken);
        return trip;
    }

    public async Task<IReadOnlyList<Trip>> GetOpenDriverTripsAsync(
        decimal pickupLat,
        decimal pickupLng,
        decimal destLat,
        decimal destLng,
        DateTimeOffset departureAt,
        double pickupRadiusKm,
        double destRadiusKm,
        int departureWindowMinutes,
        CancellationToken cancellationToken = default)
    {
        var trips = await db.Trips
            .AsNoTracking()
            .Where(t => t.Role == TripRole.Driver && t.Status == TripStatus.Open)
            .Where(t => t.DepartureAt > DateTimeOffset.UtcNow)
            .Where(t => t.DepartureAt >= departureAt.AddMinutes(-departureWindowMinutes) &&
                       t.DepartureAt <= departureAt.AddMinutes(departureWindowMinutes))
            .ToListAsync(cancellationToken);

        var cfg = options.Value;
        return trips
            .Where(t => HaversineDistanceService.DistanceKm((double)pickupLat, (double)pickupLng, (double)t.PickupLat, (double)t.PickupLng) <= pickupRadiusKm)
            .Where(t => HaversineDistanceService.DistanceKm((double)destLat, (double)destLng, (double)t.DestinationLat, (double)t.DestinationLng) <= destRadiusKm)
            .ToList();
    }

    public async Task<IReadOnlyList<Trip>> GetOpenRiderTripsAsync(
        decimal pickupLat,
        decimal pickupLng,
        decimal destLat,
        decimal destLng,
        DateTimeOffset departureAt,
        double pickupRadiusKm,
        double destRadiusKm,
        int departureWindowMinutes,
        CancellationToken cancellationToken = default)
    {
        var trips = await db.Trips
            .AsNoTracking()
            .Where(t => t.Role == TripRole.Rider && t.Status == TripStatus.Open)
            .Where(t => t.DepartureAt > DateTimeOffset.UtcNow)
            .Where(t => t.DepartureAt >= departureAt.AddMinutes(-departureWindowMinutes) &&
                       t.DepartureAt <= departureAt.AddMinutes(departureWindowMinutes))
            .ToListAsync(cancellationToken);

        return trips
            .Where(t => HaversineDistanceService.DistanceKm((double)pickupLat, (double)pickupLng, (double)t.PickupLat, (double)t.PickupLng) <= pickupRadiusKm)
            .Where(t => HaversineDistanceService.DistanceKm((double)destLat, (double)destLng, (double)t.DestinationLat, (double)t.DestinationLng) <= destRadiusKm)
            .ToList();
    }
}
