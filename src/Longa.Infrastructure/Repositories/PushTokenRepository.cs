using Longa.Application.Common.Interfaces;
using Longa.Domain.Entities;
using Longa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Longa.Infrastructure.Repositories;

public class PushTokenRepository(LongaDbContext db) : IPushTokenRepository
{
    public async Task UpsertAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var existing = await db.PushTokens.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (existing is not null)
        {
            existing.Token = token;
            existing.UpdatedAt = now;
            db.PushTokens.Update(existing);
        }
        else
        {
            db.PushTokens.Add(new PushToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
