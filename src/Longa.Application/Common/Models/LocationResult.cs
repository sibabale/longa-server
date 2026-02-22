namespace Longa.Application.Common.Models;

public record LocationResult(
    string Id,
    string Name,
    string Address,
    Coordinates Coordinates
);

public record Coordinates(double Latitude, double Longitude);
