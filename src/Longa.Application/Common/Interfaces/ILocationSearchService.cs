using Longa.Application.Common.Models;

namespace Longa.Application.Common.Interfaces;

public interface ILocationSearchService
{
    Task<IReadOnlyList<LocationSuggestion>> SuggestAsync(
        string query,
        string sessionToken,
        LocationSearchOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<LocationResult?> RetrieveAsync(
        string mapboxId,
        string sessionToken,
        CancellationToken cancellationToken = default);
}

public record LocationSearchOptions(
    string? Proximity = null,
    string? Country = null,
    int Limit = 5
);
