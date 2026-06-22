namespace Oryxen.Application.Common.Models;

/// <summary>Normalized current-weather reading returned by the weather provider.</summary>
public sealed record WeatherSnapshot(
    string Location,
    double TemperatureC,
    double HumidityPct,
    string Description,
    DateTime ObservedAtUtc);
