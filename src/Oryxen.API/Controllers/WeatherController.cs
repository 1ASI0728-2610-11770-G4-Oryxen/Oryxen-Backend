using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Weather.Contracts;
using Oryxen.Domain.Constants;

namespace Oryxen.API.Controllers;

/// <summary>
/// Third-party weather integration (OpenWeatherMap). Provides ambient temperature and
/// humidity by city name or geographic coordinates to complement IoT telemetry.
/// </summary>
[ApiController]
[Route("api/v1/weather")]
[Produces("application/json")]
[Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
public sealed class WeatherController : ControllerBase
{
    private readonly IWeatherService _weather;

    public WeatherController(IWeatherService weather) => _weather = weather;

    /// <summary>Current weather for a city or farm location name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherResponse>> GetByCity([FromQuery] string city, CancellationToken cancellationToken)
    {
        var snapshot = await _weather.GetByCityAsync(city, cancellationToken);
        return Ok(ToResponse(snapshot));
    }

    /// <summary>Current weather for explicit latitude/longitude coordinates.</summary>
    [HttpGet("by-coordinates")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherResponse>> GetByCoordinates(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken cancellationToken)
    {
        var snapshot = await _weather.GetByCoordinatesAsync(lat, lon, cancellationToken);
        return Ok(ToResponse(snapshot));
    }

    private static WeatherResponse ToResponse(Oryxen.Application.Common.Models.WeatherSnapshot s) =>
        new(s.Location, s.TemperatureC, s.HumidityPct, s.Description, s.ObservedAtUtc);
}
