using Oryxen.Application.Common.Models;

namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Abstraction over a third-party ambient weather provider (OpenWeatherMap),
/// used to complement Sensor Lite telemetry with farm-location weather.
/// </summary>
public interface IWeatherService
{
    Task<WeatherSnapshot> GetByCityAsync(string city, CancellationToken cancellationToken = default);

    Task<WeatherSnapshot> GetByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
