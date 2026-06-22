using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Plants.Contracts;
using Oryxen.Application.Weather.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.Plants;

/// <summary>
/// Orchestrates the Plant Management use cases: CRUD, Sensor Lite assignment, watering and
/// the OpenWeatherMap ambient-weather complement. Recent telemetry is reused (via
/// <see cref="ITelemetryRepository"/>) to project metrics and derive the plant status.
/// </summary>
public sealed class PlantService : IPlantService
{
    private const int RecentMetrics = 20;
    private static readonly TimeSpan DefaultWateringInterval = TimeSpan.FromDays(3);

    private readonly IPlantRepository _plants;
    private readonly ITelemetryRepository _telemetry;
    private readonly IWeatherService _weather;
    private readonly IUnitOfWork _unitOfWork;

    public PlantService(
        IPlantRepository plants,
        ITelemetryRepository telemetry,
        IWeatherService weather,
        IUnitOfWork unitOfWork)
    {
        _plants = plants;
        _telemetry = telemetry;
        _weather = weather;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PlantResponse>> GetByUserAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        var plants = await _plants.GetByUserAsync(userAccountId, cancellationToken);

        var responses = new List<PlantResponse>(plants.Count);
        foreach (var plant in plants)
        {
            responses.Add(await ToResponseAsync(plant, cancellationToken));
        }

        return responses;
    }

    public async Task<PlantResponse> GetByIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        return await ToResponseAsync(plant, cancellationToken);
    }

    public async Task<PlantResponse> CreateAsync(Guid ownerId, CreatePlantRequest request, CancellationToken cancellationToken = default)
    {
        var plant = new Plant
        {
            UserAccountId = ownerId,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            ImgUrl = request.ImgUrl?.Trim() ?? string.Empty,
            Bio = request.Bio?.Trim() ?? string.Empty,
            Location = request.Location?.Trim() ?? string.Empty,
            Status = PlantStatus.Healthy
        };

        await _plants.AddAsync(plant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(plant, cancellationToken);
    }

    public async Task<PlantResponse> UpdateAsync(Guid plantId, UpdatePlantRequest request, CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        plant.Name = request.Name.Trim();
        plant.Type = request.Type.Trim();
        plant.ImgUrl = request.ImgUrl?.Trim() ?? plant.ImgUrl;
        plant.Bio = request.Bio?.Trim() ?? plant.Bio;
        plant.Location = request.Location?.Trim() ?? plant.Location;

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<PlantStatus>(request.Status, ignoreCase: true, out var status))
        {
            plant.Status = status;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(plant, cancellationToken);
    }

    public async Task DeleteAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        _plants.Remove(plant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlantResponse> WaterAsync(Guid plantId, WaterPlantRequest request, CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        var wateredAt = request.WateredAt ?? DateTime.UtcNow;
        plant.WateringLogs.Add(new WateringLog { PlantId = plant.Id, WateredAt = wateredAt });
        plant.LastWateredAt = wateredAt;
        plant.NextWateringAt = wateredAt.Add(DefaultWateringInterval);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(plant, cancellationToken);
    }

    public async Task<PlantResponse> AssignSensorAsync(Guid plantId, AssignSensorRequest request, CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        plant.AssignedDeviceId = request.DeviceId.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(plant, cancellationToken);
    }

    public async Task<WeatherResponse> GetWeatherAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        if (string.IsNullOrWhiteSpace(plant.Location))
        {
            throw new ExternalServiceException("This plant has no farm location set; cannot resolve ambient weather.");
        }

        var snapshot = await _weather.GetByCityAsync(plant.Location, cancellationToken);

        return new WeatherResponse(
            snapshot.Location,
            snapshot.TemperatureC,
            snapshot.HumidityPct,
            snapshot.Description,
            snapshot.ObservedAtUtc);
    }

    private async Task<PlantResponse> ToResponseAsync(Plant plant, CancellationToken cancellationToken)
    {
        // Reuse the telemetry repository (newest-first) to project metrics and derive status.
        var readings = await _telemetry.GetByPlantAsync(plant.Id, from: null, to: null, cancellationToken);

        var metrics = readings
            .Take(RecentMetrics)
            .Select(r => new PlantMetricDto(
                r.Id,
                r.PlantId,
                r.DeviceId,
                r.Temperature,
                r.Humidity,
                r.LightLevel,
                r.SoilMoisture,
                r.RecordedAt))
            .ToArray();

        var wateringLogs = plant.WateringLogs
            .OrderByDescending(w => w.WateredAt)
            .Select(w => new WateringLogDto(w.Id, w.PlantId, w.WateredAt))
            .ToArray();

        var status = DeriveStatus(plant, readings);

        return new PlantResponse(
            plant.Id,
            plant.UserAccountId,
            plant.Name,
            plant.Type,
            plant.ImgUrl,
            plant.Bio,
            plant.Location,
            status.ToString(),
            plant.LastWateredAt,
            plant.NextWateringAt,
            metrics,
            wateringLogs,
            plant.CreatedAt,
            plant.UpdatedAt);
    }

    /// <summary>
    /// Links the IoT telemetry to Plant Management: when readings exist, the plant status is
    /// derived from the latest Health Score; otherwise the stored status is used.
    /// </summary>
    private static PlantStatus DeriveStatus(Plant plant, IReadOnlyList<TelemetryData> readings)
    {
        if (readings.Count == 0)
        {
            return plant.Status;
        }

        var latest = readings[0]; // repository returns newest first
        return latest.HealthScore switch
        {
            >= 70 => PlantStatus.Healthy,
            >= 40 => PlantStatus.Warning,
            _ => PlantStatus.Critical
        };
    }
}
