namespace Oryxen.Application.Plants.Contracts;

/// <summary>
/// Projection of a plant returned to the web/mobile clients. Property names are chosen to
/// serialize (camelCase) exactly as the Web Application's PlantAssembler expects.
/// </summary>
public sealed record PlantResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string Type,
    string ImgUrl,
    string Bio,
    string Location,
    string Status,
    DateTime? LastWatered,
    DateTime? NextWatering,
    IReadOnlyList<PlantMetricDto> Metrics,
    IReadOnlyList<WateringLogDto> WateringLogs,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>A recent telemetry reading, reshaped to the client's Metric contract.</summary>
public sealed record PlantMetricDto(
    Guid Id,
    Guid PlantId,
    string DeviceId,
    double AirTemperatureC,
    double AirHumidityPct,
    double LightIntensityLux,
    double SoilMoisturePct,
    DateTime Timestamp);

/// <summary>A watering log entry.</summary>
public sealed record WateringLogDto(
    Guid Id,
    Guid PlantId,
    DateTime WateredAt);
