using Longa.Domain.Entities;

namespace Longa.Application.Common.Interfaces;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip> CreateAsync(Trip trip, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trip>> GetOpenDriverTripsAsync(
        decimal pickupLat,
        decimal pickupLng,
        decimal destLat,
        decimal destLng,
        DateTimeOffset departureAt,
        double pickupRadiusKm,
        double destRadiusKm,
        int departureWindowMinutes,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trip>> GetOpenRiderTripsAsync(
        decimal pickupLat,
        decimal pickupLng,
        decimal destLat,
        decimal destLng,
        DateTimeOffset departureAt,
        double pickupRadiusKm,
        double destRadiusKm,
        int departureWindowMinutes,
        CancellationToken cancellationToken = default);
}
