using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Common.Models;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// <see cref="IWeatherService"/> backed by the OpenWeatherMap Current Weather Data API.
/// Registered as a typed HttpClient. Failures, timeouts and missing configuration surface
/// as <see cref="ExternalServiceException"/> (HTTP 502).
/// </summary>
public sealed class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _http;
    private readonly OpenWeatherMapSettings _settings;

    public OpenWeatherMapService(HttpClient http, IOptions<OpenWeatherMapSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public Task<WeatherSnapshot> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ExternalServiceException("A city or farm location is required to query the weather.");
        }

        return QueryAsync($"/data/2.5/weather?q={Uri.EscapeDataString(city)}", cancellationToken);
    }

    public Task<WeatherSnapshot> GetByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default) =>
        QueryAsync(
            $"/data/2.5/weather?lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}",
            cancellationToken);

    private async Task<WeatherSnapshot> QueryAsync(string pathAndQuery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ExternalServiceException("OpenWeatherMap API key is not configured (set OpenWeatherMap__ApiKey).");
        }

        var url = $"{pathAndQuery}&appid={_settings.ApiKey}&units={_settings.Units}";

        OwmResponse? payload;
        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceException($"OpenWeatherMap returned HTTP {(int)response.StatusCode}.");
            }

            payload = await response.Content.ReadFromJsonAsync<OwmResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("OpenWeatherMap is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("OpenWeatherMap request timed out.", ex);
        }

        if (payload?.Main is null)
        {
            throw new ExternalServiceException("OpenWeatherMap returned an unexpected payload.");
        }

        return new WeatherSnapshot(
            Location: payload.Name ?? "Unknown",
            TemperatureC: payload.Main.Temp,
            HumidityPct: payload.Main.Humidity,
            Description: payload.Weather?.FirstOrDefault()?.Description ?? string.Empty,
            ObservedAtUtc: DateTimeOffset.FromUnixTimeSeconds(payload.Dt).UtcDateTime);
    }

    // ---- OpenWeatherMap wire DTOs (internal) ---------------------------------
    private sealed record OwmResponse
    {
        [JsonPropertyName("name")] public string? Name { get; init; }

        [JsonPropertyName("dt")] public long Dt { get; init; }

        [JsonPropertyName("main")] public OwmMain? Main { get; init; }

        [JsonPropertyName("weather")] public List<OwmWeather>? Weather { get; init; }
    }

    private sealed record OwmMain
    {
        [JsonPropertyName("temp")] public double Temp { get; init; }

        [JsonPropertyName("humidity")] public double Humidity { get; init; }
    }

    private sealed record OwmWeather
    {
        [JsonPropertyName("description")] public string? Description { get; init; }
    }
}
