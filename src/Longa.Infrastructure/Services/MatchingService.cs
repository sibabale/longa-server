using Longa.Application.Common.Interfaces;
using Longa.Domain.Entities;
using Longa.Infrastructure.Data;
using Longa.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Longa.Infrastructure.Services;

public class MatchingService(
    LongaDbContext db,
    IOptions<MatchingOptions> options) : IMatchingService
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);
    private readonly MatchingOptions _options = options.Value;

    public async Task<Booking?> TryMatchDriverTripAsync(Trip driverTrip, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var riders = await db.Trips
                .Where(t => t.Role == TripRole.Rider && t.Status == TripStatus.Open)
                .Where(t => t.DepartureAt > DateTimeOffset.UtcNow)
                .Where(t => t.DepartureAt >= driverTrip.DepartureAt.AddMinutes(-_options.DepartureWindowMinutes) &&
                           t.DepartureAt <= driverTrip.DepartureAt.AddMinutes(_options.DepartureWindowMinutes))
                .OrderBy(t => t.CreatedAt)
                .Take(50)
                .Select(t => new
                {
                    t.Id,
                    t.PickupLat,
                    t.PickupLng,
                    t.DestinationLat,
                    t.DestinationLng
                })
                .ToListAsync(cancellationToken);

            foreach (var r in riders)
            {
                var pickupKm = HaversineDistanceService.DistanceKm(
                    (double)driverTrip.PickupLat, (double)driverTrip.PickupLng,
                    (double)r.PickupLat, (double)r.PickupLng);
                var destKm = HaversineDistanceService.DistanceKm(
                    (double)driverTrip.DestinationLat, (double)driverTrip.DestinationLng,
                    (double)r.DestinationLat, (double)r.DestinationLng);

                if (pickupKm <= _options.PickupRadiusKm && destKm <= _options.DestinationRadiusKm)
                {
                    var riderTrip = await db.Trips
                        .FromSqlRaw("SELECT * FROM trips WHERE id = {0} FOR UPDATE", r.Id)
                        .AsTracking()
                        .FirstOrDefaultAsync(cancellationToken);
                    if (riderTrip is null || riderTrip.Status != TripStatus.Open)
                        continue;

                    var driverForUpdate = await db.Trips
                        .FromSqlRaw("SELECT * FROM trips WHERE id = {0} FOR UPDATE", driverTrip.Id)
                        .AsTracking()
                        .FirstOrDefaultAsync(cancellationToken);
                    if (driverForUpdate is null || driverForUpdate.Status != TripStatus.Open)
                        break;

                    riderTrip.Status = TripStatus.Booked;
                    riderTrip.UpdatedAt = DateTimeOffset.UtcNow;
                    driverForUpdate.Status = TripStatus.Booked;
                    driverForUpdate.UpdatedAt = DateTimeOffset.UtcNow;

                    var booking = new Booking
                    {
                        DriverTripId = driverForUpdate.Id,
                        RiderTripId = riderTrip.Id,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return booking;
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Booking?> TryMatchRiderTripAsync(Trip riderTrip, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var drivers = await db.Trips
                .Where(t => t.Role == TripRole.Driver && t.Status == TripStatus.Open)
                .Where(t => t.DepartureAt > DateTimeOffset.UtcNow)
                .Where(t => t.DepartureAt >= riderTrip.DepartureAt.AddMinutes(-_options.DepartureWindowMinutes) &&
                           t.DepartureAt <= riderTrip.DepartureAt.AddMinutes(_options.DepartureWindowMinutes))
                .OrderBy(t => t.CreatedAt)
                .Take(50)
                .Select(t => new
                {
                    t.Id,
                    t.PickupLat,
                    t.PickupLng,
                    t.DestinationLat,
                    t.DestinationLng
                })
                .ToListAsync(cancellationToken);

            foreach (var d in drivers)
            {
                var pickupKm = HaversineDistanceService.DistanceKm(
                    (double)riderTrip.PickupLat, (double)riderTrip.PickupLng,
                    (double)d.PickupLat, (double)d.PickupLng);
                var destKm = HaversineDistanceService.DistanceKm(
                    (double)riderTrip.DestinationLat, (double)riderTrip.DestinationLng,
                    (double)d.DestinationLat, (double)d.DestinationLng);

                if (pickupKm <= _options.PickupRadiusKm && destKm <= _options.DestinationRadiusKm)
                {
                    var driverTrip = await db.Trips
                        .FromSqlRaw("SELECT * FROM trips WHERE id = {0} FOR UPDATE", d.Id)
                        .AsTracking()
                        .FirstOrDefaultAsync(cancellationToken);
                    if (driverTrip is null || driverTrip.Status != TripStatus.Open)
                        continue;

                    var riderForUpdate = await db.Trips
                        .FromSqlRaw("SELECT * FROM trips WHERE id = {0} FOR UPDATE", riderTrip.Id)
                        .AsTracking()
                        .FirstOrDefaultAsync(cancellationToken);
                    if (riderForUpdate is null || riderForUpdate.Status != TripStatus.Open)
                        break;

                    riderForUpdate.Status = TripStatus.Booked;
                    riderForUpdate.UpdatedAt = DateTimeOffset.UtcNow;
                    driverTrip.Status = TripStatus.Booked;
                    driverTrip.UpdatedAt = DateTimeOffset.UtcNow;

                    var booking = new Booking
                    {
                        DriverTripId = driverTrip.Id,
                        RiderTripId = riderForUpdate.Id,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return booking;
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
