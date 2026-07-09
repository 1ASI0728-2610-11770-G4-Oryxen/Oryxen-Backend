using Oryxen.Application.Analytics.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;
using Oryxen.Domain.ValueObjects;

namespace Oryxen.Application.Analytics;

public sealed class AnalysisService : IAnalysisService
{
    private readonly IPlantRepository _plantRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IAnalysisReportRepository _reportRepository;

    public AnalysisService(
        IPlantRepository plantRepository,
        ITelemetryRepository telemetryRepository,
        IAnalysisReportRepository reportRepository)
    {
        _plantRepository = plantRepository;
        _telemetryRepository = telemetryRepository;
        _reportRepository = reportRepository;
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var metrics = await _reportRepository.GetDashboardMetricsAsync(userAccountId, since, cancellationToken);
        var metricList = metrics.ToList();

        if (metricList.Count == 0)
            return new DashboardResponse(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<PlantHealthSummary>());

        var withData = metricList.Where(m => m.ReadingCount > 0).ToList();
        var count = withData.Count == 0 ? 1 : withData.Count;

        var summaries = metricList.Select(m => new PlantHealthSummary(
            m.PlantId, m.PlantName, m.PlantType, m.Status,
            m.AvgHealthScore, m.AvgSoilMoisture, m.ReadingCount, m.LastReadingAt)).ToList();

        var healthy = metricList.Count(m => m.Status == "healthy");
        var warning = metricList.Count(m => m.Status == "warning");
        var critical = metricList.Count(m => m.Status == "critical");

        return new DashboardResponse(
            metricList.Count, healthy, warning, critical,
            Math.Round(withData.Sum(m => m.AvgHumidity) / count, 1),
            Math.Round(withData.Sum(m => m.AvgTemperature) / count, 1),
            Math.Round(withData.Sum(m => m.AvgSoilMoisture) / count, 1),
            Math.Round(withData.Sum(m => m.AvgLightLevel) / count, 1),
            Math.Round(withData.Sum(m => m.AvgHealthScore) / count, 1),
            metricList.Sum(m => m.ReadingCount),
            summaries);
    }

    public async Task<PlantTrendResponse> GetPlantTrendsAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken);
        if (plant is null)
            return new PlantTrendResponse(plantId, string.Empty,
                Array.Empty<TrendPoint>(), Array.Empty<TrendPoint>(), Array.Empty<TrendPoint>());

        var now = DateTime.UtcNow;

        var dailyProjections = await _reportRepository.GetDailyTrendsAsync(plantId, now.AddDays(-7), cancellationToken);
        var weeklyProjections = await _reportRepository.GetWeeklyTrendsAsync(plantId, now.AddDays(-56), cancellationToken);
        var monthlyProjections = await _reportRepository.GetMonthlyTrendsAsync(plantId, now.AddMonths(-6), cancellationToken);

        var daily = dailyProjections.Select(MapTrendPoint).ToList();
        var weekly = weeklyProjections.Select(MapTrendPoint).ToList();
        var monthly = monthlyProjections.Select(MapTrendPoint).ToList();

        return new PlantTrendResponse(plantId, plant.Name, daily, weekly, monthly);
    }

    public async Task<ReportListResponse> GetReportsAsync(
        Guid userAccountId, Guid? plantId = null, int page = 1, int size = 20,
        CancellationToken cancellationToken = default)
    {
        var projections = await _reportRepository.GetReportProjectionsAsync(userAccountId, plantId, page, size, cancellationToken);
        var total = await _reportRepository.CountByUserAsync(userAccountId, plantId, cancellationToken);

        var items = new List<ReportItemResponse>();
        foreach (var r in projections)
        {
            var plantName = string.Empty;
            try
            {
                var p = await _plantRepository.GetByIdAsync(r.PlantId, cancellationToken);
                plantName = p?.Name ?? string.Empty;
            }
            catch { }

            items.Add(new ReportItemResponse(
                r.Id, r.PlantId, plantName,
                r.Type, r.Status, r.Format,
                r.RangeStart, r.RangeEnd, r.CreatedAt, r.GeneratedAt));
        }

        return new ReportListResponse(items, total, page, size);
    }

    public async Task<ReportDetailResponse> GenerateReportAsync(
        Guid userAccountId, GenerateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(request.PlantId, cancellationToken);
        var reportType = Enum.TryParse<ReportType>(request.Type, true, out var t) ? t : ReportType.HealthSummary;
        var exportFormat = Enum.TryParse<ExportFormat>(request.Format, true, out var f) ? f : ExportFormat.Csv;

        var report = new AnalysisReport
        {
            UserAccountId = userAccountId,
            PlantId = request.PlantId,
            Type = reportType,
            Status = ReportStatus.Processing,
            RangeStart = request.RangeStart,
            RangeEnd = request.RangeEnd,
            Format = exportFormat,
            CreatedAt = DateTime.UtcNow
        };

        await _reportRepository.AddAsync(report, cancellationToken);

        var readings = await _telemetryRepository.GetByPlantAsync(
            request.PlantId, request.RangeStart, request.RangeEnd, cancellationToken);
        var readingList = readings.ToList();

        report.FileContent = exportFormat == ExportFormat.Csv
            ? SerializeCsv(readingList)
            : SerializeJson(readingList);
        report.Status = ReportStatus.Completed;
        report.GeneratedAt = DateTime.UtcNow;

        return new ReportDetailResponse(
            report.Id, report.PlantId, plant?.Name ?? string.Empty,
            report.Type.ToString(), report.Status.ToString(), report.Format.ToString(),
            report.RangeStart, report.RangeEnd, report.FileContent,
            report.CreatedAt, report.GeneratedAt);
    }

    public async Task<ReportDetailResponse?> GetReportByIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null) return null;

        var plantName = string.Empty;
        try
        {
            var p = await _plantRepository.GetByIdAsync(report.PlantId, cancellationToken);
            plantName = p?.Name ?? string.Empty;
        }
        catch { }

        return new ReportDetailResponse(
            report.Id, report.PlantId, plantName,
            report.Type.ToString(), report.Status.ToString(), report.Format.ToString(),
            report.RangeStart, report.RangeEnd, report.FileContent,
            report.CreatedAt, report.GeneratedAt);
    }

    private static TrendPoint MapTrendPoint(TrendPointProjection p) =>
        new(p.Label, p.AvgHealthScore, p.AvgSoilMoisture, p.AvgTemperature, p.AvgHumidity, p.ReadingCount);

    private static string SerializeCsv(IEnumerable<TelemetryData> readings)
    {
        var header = "RecordedAt,DeviceId,HealthScore,SoilMoisture,Temperature,Humidity,LightLevel";
        var rows = readings.Select(r =>
            $"{r.RecordedAt:O},{r.DeviceId},{r.HealthScore},{r.SoilMoisture},{r.Temperature},{r.Humidity},{r.LightLevel}");
        return string.Join(Environment.NewLine, new[] { header }.Concat(rows));
    }

    private static string SerializeJson(IEnumerable<TelemetryData> readings)
    {
        var items = readings.Select(r =>
            $"{{\"recordedAt\":\"{r.RecordedAt:O}\",\"deviceId\":\"{r.DeviceId}\",\"healthScore\":{r.HealthScore},\"soilMoisture\":{r.SoilMoisture},\"temperature\":{r.Temperature},\"humidity\":{r.Humidity},\"lightLevel\":{r.LightLevel}}}");
        return $"[{string.Join(",", items)}]";
    }
}
