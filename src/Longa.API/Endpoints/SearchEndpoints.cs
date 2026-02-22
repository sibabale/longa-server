using Longa.Application.Common.Interfaces;
using Longa.Application.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Longa.API;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/search/suggest", Suggest)
            .WithName("SuggestLocations")
            .Produces<IReadOnlyList<SuggestResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapGet("/search/retrieve/{mapboxId}", Retrieve)
            .WithName("RetrieveLocation")
            .Produces<RetrieveResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> Suggest(
        ILocationSearchService search,
        [FromQuery] string q,
        [FromQuery(Name = "session_token")] string sessionToken,
        [FromQuery] string? proximity,
        [FromQuery] string? country,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return ErrorResults.BadRequest("Search query is required.");
        if (string.IsNullOrWhiteSpace(sessionToken))
            return ErrorResults.BadRequest("Session token is required.");

        var options = new LocationSearchOptions(
            Proximity: proximity,
            Country: country,
            Limit: limit ?? 5);

        var suggestions = await search.SuggestAsync(
            q,
            sessionToken,
            options,
            cancellationToken);

        var response = suggestions.Select(s => new SuggestResponse(
            s.MapboxId,
            s.Name,
            s.FullAddress,
            s.PlaceFormatted,
            s.FeatureType));

        return Results.Ok(response.ToList());
    }

    private static async Task<IResult> Retrieve(
        ILocationSearchService search,
        string mapboxId,
        [FromQuery(Name = "session_token")] string sessionToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mapboxId))
            return ErrorResults.BadRequest("Location ID is required.");
        if (string.IsNullOrWhiteSpace(sessionToken))
            return ErrorResults.BadRequest("Session token is required.");

        var result = await search.RetrieveAsync(
            mapboxId,
            sessionToken,
            cancellationToken);

        if (result is null)
            return ErrorResults.NotFound("Location not found.");

        return Results.Ok(new RetrieveResponse(
            result.Id,
            result.Name,
            result.Address,
            new CoordinateResponse(result.Coordinates.Latitude, result.Coordinates.Longitude)));
    }
}

public record SuggestResponse(
    string MapboxId,
    string Name,
    string? FullAddress,
    string PlaceFormatted,
    string FeatureType);

public record RetrieveResponse(
    string Id,
    string Name,
    string Address,
    CoordinateResponse Coordinates);

public record CoordinateResponse(double Latitude, double Longitude);
