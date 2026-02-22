namespace Longa.Application.Common.Models;

public record LocationSuggestion(
    string MapboxId,
    string Name,
    string? FullAddress,
    string PlaceFormatted,
    string FeatureType
);
