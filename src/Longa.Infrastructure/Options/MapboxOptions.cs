namespace Longa.Infrastructure.Options;

public class MapboxOptions
{
    public const string SectionName = "Mapbox";

    public string AccessToken { get; set; } = string.Empty;
}
