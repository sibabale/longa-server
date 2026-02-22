namespace Longa.Infrastructure.Options;

public class MatchingOptions
{
    public const string SectionName = "Matching";

    public double PickupRadiusKm { get; set; } = 10;
    public double DestinationRadiusKm { get; set; } = 10;
    public int DepartureWindowMinutes { get; set; } = 120;
}
