namespace Longa.Application.Common.Interfaces;

public interface IIdempotencyRepository
{
    Task<(bool Exists, Guid? TripId)> TryGetExistingAsync(Guid key, Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task StoreAsync(Guid key, Guid userId, Guid tripId, CancellationToken cancellationToken = default);
}
