namespace Oryxen.Infrastructure.External;

/// <summary>
/// Binds the "OpenWeatherMap" configuration section. The API key must be supplied via the
/// environment variable <c>OpenWeatherMap__ApiKey</c> (never committed to source control).
/// </summary>
public sealed class OpenWeatherMapSettings
{
    public const string SectionName = "OpenWeatherMap";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openweathermap.org";

    public string Units { get; set; } = "metric";
}
