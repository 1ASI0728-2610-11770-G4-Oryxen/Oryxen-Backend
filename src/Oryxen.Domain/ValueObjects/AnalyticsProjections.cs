namespace Oryxen.Domain.ValueObjects;

/// <summary>
/// Lightweight projection returned by repository aggregation queries.
/// Belongs to the Domain layer to keep Application free of infrastructure concerns.
/// </summary>
public sealed record PlantMetricProjection(
    Guid PlantId,
    string PlantName,
    string PlantType,
    string Status,
    double AvgHealthScore,
    double AvgSoilMoisture,
    double AvgTemperature,
    double AvgHumidity,
    double AvgLightLevel,
    int ReadingCount,
    DateTime? LastReadingAt);

public sealed record TrendPointProjection(
    string Label,
    double AvgHealthScore,
    double AvgSoilMoisture,
    double AvgTemperature,
    double AvgHumidity,
    int ReadingCount);

public sealed record ReportProjection(
    Guid Id,
    Guid PlantId,
    string Type,
    string Status,
    string Format,
    DateTime RangeStart,
    DateTime RangeEnd,
    DateTime CreatedAt,
    DateTime? GeneratedAt);
