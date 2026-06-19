namespace Oryxen.Application.Telemetry.Contracts;

public sealed record TelemetryResponse(
    Guid Id,
    string DeviceId,
    Guid PlantId,
    double Humidity,
    double Temperature,
    double LightLevel,
    double SoilMoisture,
    int HealthScore,
    DateTime RecordedAt);
