namespace Oryxen.Application.Weather.Contracts;

/// <summary>Current ambient weather for a location, returned by the weather endpoints.</summary>
public sealed record WeatherResponse(
    string Location,
    double TemperatureC,
    double HumidityPct,
    string Description,
    DateTime ObservedAtUtc);
