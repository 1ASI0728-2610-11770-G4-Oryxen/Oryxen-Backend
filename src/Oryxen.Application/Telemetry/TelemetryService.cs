using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Notifications;
using Oryxen.Application.Notifications.Contracts;
using Oryxen.Application.Telemetry.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;
using Oryxen.Domain.Services;

namespace Oryxen.Application.Telemetry;

public sealed class TelemetryService : ITelemetryService
{
    private readonly ITelemetryRepository _telemetry;
    private readonly IPlantRepository _plants;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public TelemetryService(
        ITelemetryRepository telemetry,
        IPlantRepository plants,
        INotificationService notifications,
        IUnitOfWork unitOfWork)
    {
        _telemetry = telemetry;
        _plants = plants;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task<TelemetryResponse> IngestAsync(TelemetryIngestRequest request, CancellationToken cancellationToken = default)
    {
        var healthScore = PlantHealthCalculator.Compute(request.SoilMoisture, request.Humidity, request.Temperature);

        var reading = new TelemetryData
        {
            DeviceId = request.DeviceId.Trim(),
            PlantId = request.PlantId,
            Humidity = request.Humidity,
            Temperature = request.Temperature,
            LightLevel = request.LightLevel,
            SoilMoisture = request.SoilMoisture,
            HealthScore = healthScore,
            RecordedAt = request.RecordedAt ?? DateTime.UtcNow
        };

        await _telemetry.AddAsync(reading, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (healthScore < 40)
        {
            await CreateCriticalAlertAsync(request.PlantId, healthScore, cancellationToken);
        }

        return ToResponse(reading);
    }

    public async Task<IReadOnlyList<TelemetryResponse>> GetByPlantAsync(
        Guid plantId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var readings = await _telemetry.GetByPlantAsync(plantId, from, to, cancellationToken);
        return readings.Select(ToResponse).ToArray();
    }

    private async Task CreateCriticalAlertAsync(Guid plantId, double healthScore, CancellationToken cancellationToken)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken);
        if (plant is null)
            return;

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId = plant.UserAccountId,
            PlantId = plantId,
            Type = NotificationType.CriticalHealth,
            Channel = NotificationChannel.InApp,
            Title = "Alerta Crítica de Salud",
            Message = $"La salud de {plant.Name} ha caído a {healthScore:F1}%. Revise la planta y tome medidas correctivas."
        }, cancellationToken);
    }

    private static TelemetryResponse ToResponse(TelemetryData reading) =>
        new(
            reading.Id,
            reading.DeviceId,
            reading.PlantId,
            reading.Humidity,
            reading.Temperature,
            reading.LightLevel,
            reading.SoilMoisture,
            reading.HealthScore,
            reading.RecordedAt);
}
