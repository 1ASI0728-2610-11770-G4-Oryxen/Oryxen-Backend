using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Telemetry.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;
using Oryxen.Domain.Services;

namespace Oryxen.Application.Telemetry;

/// <summary>
/// Application service for the IoT telemetry ingestion flow. Persists Sensor Lite
/// readings and derives a plant Health Score for each one via the domain calculator.
/// </summary>
public sealed class TelemetryService : ITelemetryService
{
    private readonly ITelemetryRepository _telemetry;
    private readonly IUnitOfWork _unitOfWork;

    public TelemetryService(ITelemetryRepository telemetry, IUnitOfWork unitOfWork)
    {
        _telemetry = telemetry;
        _unitOfWork = unitOfWork;
    }

    public async Task<TelemetryResponse> IngestAsync(TelemetryIngestRequest request, CancellationToken cancellationToken = default)
    {
        var reading = new TelemetryData
        {
            DeviceId = request.DeviceId.Trim(),
            PlantId = request.PlantId,
            Humidity = request.Humidity,
            Temperature = request.Temperature,
            LightLevel = request.LightLevel,
            SoilMoisture = request.SoilMoisture,
            HealthScore = PlantHealthCalculator.Compute(request.SoilMoisture, request.Humidity, request.Temperature),
            RecordedAt = request.RecordedAt ?? DateTime.UtcNow
        };

        await _telemetry.AddAsync(reading, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
