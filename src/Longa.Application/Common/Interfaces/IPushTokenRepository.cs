namespace Longa.Application.Common.Interfaces;

public interface IPushTokenRepository
{
    Task UpsertAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}
