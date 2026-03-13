using Longa.Application.Common.Interfaces;
using Longa.Domain.Entities;
using Longa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Longa.Infrastructure.Repositories;

public class UserRepository(LongaDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdentifierForVendorAsync(string identifierForVendor, CancellationToken cancellationToken = default)
    {
        return await db.Users
            .FirstOrDefaultAsync(u => u.IdentifierForVendor == identifierForVendor, cancellationToken);
    }

    public async Task<User?> GetByAuth0UserIdAsync(string Auth0UserId, CancellationToken cancellationToken = default){
        return await db.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == Auth0UserId, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        user.CreatedAt = now;
        user.UpdatedAt = now;
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.Users.Update(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
