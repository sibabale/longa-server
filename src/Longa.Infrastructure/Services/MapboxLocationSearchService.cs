using System.Text.Json;
using Longa.Application.Common.Interfaces;
using Longa.Application.Common.Models;
using Longa.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Longa.Infrastructure.Services;

public class MapboxLocationSearchService : ILocationSearchService
{
    private const string MapboxBaseUrl = "https://api.mapbox.com/search/searchbox/v1";
    private readonly HttpClient _httpClient;
    private readonly MapboxOptions _options;

    public MapboxLocationSearchService(HttpClient httpClient, IOptions<MapboxOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<LocationSuggestion>> SuggestAsync(
        string query,
        string sessionToken,
        LocationSearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<LocationSuggestion>();

        var opts = options ?? new LocationSearchOptions();

        var queryParams = new List<KeyValuePair<string, string>>
        {
            new("q", query.Trim()),
            new("access_token", _options.AccessToken),
            new("session_token", sessionToken),
            new("limit", opts.Limit.ToString()),
            new("language", "en")
        };

        if (!string.IsNullOrEmpty(opts.Proximity))
            queryParams.Add(new("proximity", opts.Proximity));
        if (!string.IsNullOrEmpty(opts.Country))
            queryParams.Add(new("country", opts.Country));

        var queryString = string.Join("&", queryParams.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var response = await _httpClient.GetAsync(
            $"{MapboxBaseUrl}/suggest?{queryString}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var suggestions = new List<LocationSuggestion>();
        if (doc.RootElement.TryGetProperty("suggestions", out var suggestionsElement))
        {
            foreach (var s in suggestionsElement.EnumerateArray())
            {
                suggestions.Add(new LocationSuggestion(
                    MapboxId: s.GetProperty("mapbox_id").GetString() ?? "",
                    Name: s.GetProperty("name").GetString() ?? "",
                    FullAddress: s.TryGetProperty("full_address", out var fa) ? fa.GetString() : null,
                    PlaceFormatted: s.GetProperty("place_formatted").GetString() ?? "",
                    FeatureType: s.GetProperty("feature_type").GetString() ?? ""
                ));
            }
        }

        return suggestions;
    }

    public async Task<LocationResult?> RetrieveAsync(
        string mapboxId,
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string>>
        {
            new("access_token", _options.AccessToken),
            new("session_token", sessionToken)
        };

        var queryString = string.Join("&", queryParams.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var response = await _httpClient.GetAsync(
            $"{MapboxBaseUrl}/retrieve/{Uri.EscapeDataString(mapboxId)}?{queryString}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("features", out var features) ||
            features.GetArrayLength() == 0)
            return null;

        var feature = features[0];
        var props = feature.GetProperty("properties");
        var geom = feature.GetProperty("geometry");
        var coords = geom.GetProperty("coordinates");

        var longitude = coords[0].GetDouble();
        var latitude = coords[1].GetDouble();

        var name = props.GetProperty("name").GetString() ?? "";
        var fullAddress = props.TryGetProperty("full_address", out var fa) ? fa.GetString() : null;
        var placeFormatted = props.TryGetProperty("place_formatted", out var pf) ? pf.GetString() : null;
        var address = fullAddress ?? placeFormatted ?? name;

        return new LocationResult(
            Id: props.GetProperty("mapbox_id").GetString() ?? mapboxId,
            Name: name,
            Address: address ?? "",
            Coordinates: new Coordinates(latitude, longitude)
        );
    }
}
