using Longa.Domain.Entities;

namespace Longa.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdentifierForVendorAsync(string identifierForVendor, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByAuth0UserIdAsync(string Auth0UserId, CancellationToken cancellationToken = default);
}
