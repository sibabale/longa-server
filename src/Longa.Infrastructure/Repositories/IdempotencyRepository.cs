using Longa.Application.Common.Interfaces;
using Longa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Longa.Infrastructure.Repositories;

public class IdempotencyRepository(LongaDbContext db) : IIdempotencyRepository
{
    public async Task<(bool Exists, Guid? TripId)> TryGetExistingAsync(Guid key, Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - ttl;
        var existing = await db.IdempotencyKeys
            .AsNoTracking()
            .Where(k => k.Key == key && k.UserId == userId && k.CreatedAt > cutoff)
            .Select(k => k.TripId)
            .FirstOrDefaultAsync(cancellationToken);

        return existing != default ? (true, existing) : (false, null);
    }

    public async Task StoreAsync(Guid key, Guid userId, Guid tripId, CancellationToken cancellationToken = default)
    {
        db.IdempotencyKeys.Add(new Domain.Entities.IdempotencyKey
        {
            Key = key,
            UserId = userId,
            TripId = tripId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
