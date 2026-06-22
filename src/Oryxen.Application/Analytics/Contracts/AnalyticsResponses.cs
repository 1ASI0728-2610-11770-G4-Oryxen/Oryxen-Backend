namespace Oryxen.Application.Analytics.Contracts;

public sealed record DashboardResponse(
    int TotalPlants,
    int HealthyPlants,
    int WarningPlants,
    int CriticalPlants,
    double AvgHumidity,
    double AvgTemperature,
    double AvgSoilMoisture,
    double AvgLightLevel,
    double AvgHealthScore,
    int TotalReadings,
    IReadOnlyList<PlantHealthSummary> PlantSummaries);

public sealed record PlantHealthSummary(
    Guid PlantId,
    string PlantName,
    string PlantType,
    string Status,
    double AvgHealthScore,
    double AvgSoilMoisture,
    int ReadingCount,
    DateTime? LastReadingAt);

public sealed record PlantTrendResponse(
    Guid PlantId,
    string PlantName,
    IReadOnlyList<TrendPoint> Daily,
    IReadOnlyList<TrendPoint> Weekly,
    IReadOnlyList<TrendPoint> Monthly);

public sealed record TrendPoint(
    string Label,
    double AvgHealthScore,
    double AvgSoilMoisture,
    double AvgTemperature,
    double AvgHumidity,
    int ReadingCount);

public sealed record ReportListResponse(
    IReadOnlyList<ReportItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ReportItemResponse(
    Guid Id,
    Guid PlantId,
    string PlantName,
    string Type,
    string Status,
    string Format,
    DateTime RangeStart,
    DateTime RangeEnd,
    DateTime CreatedAt,
    DateTime? GeneratedAt);

public sealed record ReportDetailResponse(
    Guid Id,
    Guid PlantId,
    string PlantName,
    string Type,
    string Status,
    string Format,
    DateTime RangeStart,
    DateTime RangeEnd,
    string? FileContent,
    DateTime CreatedAt,
    DateTime? GeneratedAt);

public sealed record GenerateReportRequest(
    Guid PlantId,
    DateTime RangeStart,
    DateTime RangeEnd,
    string Type = "HealthSummary",
    string Format = "Csv");
