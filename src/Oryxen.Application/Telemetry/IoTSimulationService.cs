using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Telemetry.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;
using Oryxen.Domain.Services;

namespace Oryxen.Application.Telemetry;

public sealed class IoTSimulationService : IIoTSimulationService
{
    private readonly IPlantRepository _plants;
    private readonly ITelemetryRepository _telemetry;
    private readonly ITelemetryService _telemetryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IoTSimulationService> _logger;

    public IoTSimulationService(
        IPlantRepository plants,
        ITelemetryRepository telemetry,
        ITelemetryService telemetryService,
        IUnitOfWork unitOfWork,
        ILogger<IoTSimulationService> logger)
    {
        _plants = plants;
        _telemetry = telemetry;
        _telemetryService = telemetryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SeedResultResponse> SeedHistoricalDataAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allPlants = await _plants.GetAllActiveAsync(cancellationToken);
        int totalReadings = 0;

        _logger.LogInformation("Starting historical IoT data seeding for {Count} plants over {Days} days.", allPlants.Count, days);

        foreach (var plant in allPlants)
        {
            var readings = new List<TelemetryData>();
            DateTime current = DateTime.UtcNow.AddDays(-days);
            DateTime end = DateTime.UtcNow;
            
            SimulatedReading? lastReading = null;

            while (current < end)
            {
                var simReading = lastReading is null 
                    ? IoTMetricGenerator.SeedReading(current)
                    : IoTMetricGenerator.NextReading(lastReading, current);

                // Check if there was a watering near this time
                var recentWatering = plant.WateringLogs.FirstOrDefault(w => 
                    w.WateredAt >= current.AddMinutes(-30) && w.WateredAt <= current.AddMinutes(30));

                if (recentWatering != null && lastReading != null)
                {
                    simReading = IoTMetricGenerator.PostWateringReading(lastReading, current);
                }

                var healthScore = PlantHealthCalculator.Compute(
                    simReading.SoilMoisture, simReading.Humidity, simReading.Temperature);

                var deviceId = !string.IsNullOrWhiteSpace(plant.AssignedDeviceId) 
                    ? plant.AssignedDeviceId 
                    : $"sim-dev-{plant.Id.ToString().Substring(0, 8)}";

                readings.Add(new TelemetryData
                {
                    DeviceId = deviceId,
                    PlantId = plant.Id,
                    Humidity = simReading.Humidity,
                    Temperature = simReading.Temperature,
                    LightLevel = simReading.LightLevel,
                    SoilMoisture = simReading.SoilMoisture,
                    HealthScore = healthScore,
                    RecordedAt = simReading.Timestamp
                });

                lastReading = simReading;
                current = current.AddHours(1);
            }

            await _telemetry.AddRangeAsync(readings, cancellationToken);
            totalReadings += readings.Count;

            // Update plant status based on the latest generated reading
            if (readings.Any())
            {
                var latest = readings.Last();
                plant.Status = latest.HealthScore switch
                {
                    >= 70 => PlantStatus.Healthy,
                    >= 40 => PlantStatus.Warning,
                    _ => PlantStatus.Critical
                };
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation("Completed IoT data seeding in {Elapsed}. Generated {Readings} readings.", stopwatch.Elapsed, totalReadings);
        
        return new SeedResultResponse(allPlants.Count, totalReadings, stopwatch.Elapsed);
    }

    public async Task GenerateRealtimeReadingsAsync(CancellationToken cancellationToken = default)
    {
        var allPlants = await _plants.GetAllActiveAsync(cancellationToken);
        int generatedCount = 0;

        foreach (var plant in allPlants)
        {
            var latestTelemetry = await _telemetry.GetLatestByPlantAsync(plant.Id, cancellationToken);
            
            SimulatedReading? previousReading = latestTelemetry != null
                ? new SimulatedReading(
                    latestTelemetry.Temperature, 
                    latestTelemetry.Humidity, 
                    latestTelemetry.LightLevel, 
                    latestTelemetry.SoilMoisture, 
                    latestTelemetry.RecordedAt)
                : null;

            var targetTime = DateTime.UtcNow;

            var newReading = previousReading is null
                ? IoTMetricGenerator.SeedReading(targetTime)
                : IoTMetricGenerator.NextReading(previousReading, targetTime);

            // Check if watered very recently (e.g. in the last 15 mins)
            var wateredRecently = plant.WateringLogs.Any(w => 
                w.WateredAt >= targetTime.AddMinutes(-15));

            if (wateredRecently && previousReading != null)
            {
                newReading = IoTMetricGenerator.PostWateringReading(previousReading, targetTime);
            }

            var deviceId = !string.IsNullOrWhiteSpace(plant.AssignedDeviceId) 
                ? plant.AssignedDeviceId 
                : $"sim-dev-{plant.Id.ToString().Substring(0, 8)}";

            var ingestRequest = new TelemetryIngestRequest
            {
                DeviceId = deviceId,
                PlantId = plant.Id,
                Humidity = newReading.Humidity,
                Temperature = newReading.Temperature,
                LightLevel = newReading.LightLevel,
                SoilMoisture = newReading.SoilMoisture,
                RecordedAt = newReading.Timestamp
            };

            // Using TelemetryService.IngestAsync so that HealthScore is calculated 
            // and critical alerts are properly created via NotificationService
            await _telemetryService.IngestAsync(ingestRequest, cancellationToken);
            
            // Note: IngestAsync calls SaveChangesAsync internally
            
            // Update plant status based on the new reading
            var healthScore = PlantHealthCalculator.Compute(
                newReading.SoilMoisture, newReading.Humidity, newReading.Temperature);
            
            plant.Status = healthScore switch
            {
                >= 70 => PlantStatus.Healthy,
                >= 40 => PlantStatus.Warning,
                _ => PlantStatus.Critical
            };
            
            generatedCount++;
        }
        
        if (generatedCount > 0)
        {
             await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Generated {Count} realtime IoT readings.", generatedCount);
    }
}
