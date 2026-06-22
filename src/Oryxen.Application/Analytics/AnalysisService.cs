using Oryxen.Application.Analytics.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;

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
        var plants = await _plantRepository.GetByUserAsync(userAccountId, cancellationToken);
        var plantList = plants.ToList();

        if (plantList.Count == 0)
        {
            return new DashboardResponse(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                Array.Empty<PlantHealthSummary>());
        }

        var summaries = new List<PlantHealthSummary>();
        double totalHumidity = 0, totalTemperature = 0, totalSoilMoisture = 0, totalLight = 0, totalHealth = 0;
        int totalReadings = 0, healthy = 0, warning = 0, critical = 0;

        foreach (var plant in plantList)
        {
            var readings = await _telemetryRepository.GetByPlantAsync(
                plant.Id, DateTime.UtcNow.AddDays(-30), null, cancellationToken);

            var readingList = readings.ToList();

            if (readingList.Count == 0)
            {
                summaries.Add(new PlantHealthSummary(
                    plant.Id, plant.Name, plant.Type,
                    plant.Status.ToString().ToLowerInvariant(),
                    0, 0, 0, null));
                continue;
            }

            var avgHealth = readingList.Average(r => r.HealthScore);
            var avgSoil = readingList.Average(r => r.SoilMoisture);

            summaries.Add(new PlantHealthSummary(
                plant.Id, plant.Name, plant.Type,
                plant.Status.ToString().ToLowerInvariant(),
                Math.Round(avgHealth, 1),
                Math.Round(avgSoil, 1),
                readingList.Count,
                readingList.Max(r => r.RecordedAt)));

            totalHumidity += readingList.Average(r => r.Humidity);
            totalTemperature += readingList.Average(r => r.Temperature);
            totalSoilMoisture += avgSoil;
            totalLight += readingList.Average(r => r.LightLevel);
            totalHealth += avgHealth;
            totalReadings += readingList.Count;
        }

        healthy = plantList.Count(p => p.Status == PlantStatus.Healthy);
        warning = plantList.Count(p => p.Status == PlantStatus.Warning);
        critical = plantList.Count(p => p.Status == PlantStatus.Critical);

        var plantCount = summaries.Count(s => s.ReadingCount > 0);
        if (plantCount == 0) plantCount = 1;

        return new DashboardResponse(
            plantList.Count, healthy, warning, critical,
            Math.Round(totalHumidity / plantCount, 1),
            Math.Round(totalTemperature / plantCount, 1),
            Math.Round(totalSoilMoisture / plantCount, 1),
            Math.Round(totalLight / plantCount, 1),
            Math.Round(totalHealth / plantCount, 1),
            totalReadings,
            summaries);
    }

    public async Task<PlantTrendResponse> GetPlantTrendsAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _plantRepository.GetByIdAsync(plantId, cancellationToken);
        if (plant is null)
        {
            return new PlantTrendResponse(plantId, string.Empty,
                Array.Empty<TrendPoint>(), Array.Empty<TrendPoint>(), Array.Empty<TrendPoint>());
        }

        var now = DateTime.UtcNow;
        var from = now.AddDays(-90);

        var readings = await _telemetryRepository.GetByPlantAsync(plantId, from, null, cancellationToken);
        var list = readings.OrderBy(r => r.RecordedAt).ToList();

        var daily = AggregateByDay(list, now);
        var weekly = AggregateByWeek(list, now);
        var monthly = AggregateByMonth(list, now);

        return new PlantTrendResponse(plantId, plant.Name, daily, weekly, monthly);
    }

    public async Task<ReportListResponse> GetReportsAsync(
        Guid userAccountId,
        Guid? plantId = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetByUserAsync(userAccountId, plantId, page, size, cancellationToken);
        var total = await _reportRepository.CountByUserAsync(userAccountId, plantId, cancellationToken);

        var items = new List<ReportItemResponse>();
        foreach (var r in reports)
        {
            string plantName = string.Empty;
            try
            {
                var p = await _plantRepository.GetByIdAsync(r.PlantId, cancellationToken);
                plantName = p?.Name ?? string.Empty;
            }
            catch
            {
            }

            items.Add(new ReportItemResponse(
                r.Id, r.PlantId, plantName,
                r.Type.ToString(), r.Status.ToString(), r.Format.ToString(),
                r.RangeStart, r.RangeEnd, r.CreatedAt, r.GeneratedAt));
        }

        return new ReportListResponse(items, total, page, size);
    }

    public async Task<ReportDetailResponse> GenerateReportAsync(
        Guid userAccountId,
        GenerateReportRequest request,
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

        var csv = exportFormat == ExportFormat.Csv
            ? GenerateCsv(readingList)
            : GenerateJson(readingList);

        report.FileContent = csv;
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

        string plantName = string.Empty;
        try
        {
            var p = await _plantRepository.GetByIdAsync(report.PlantId, cancellationToken);
            plantName = p?.Name ?? string.Empty;
        }
        catch
        {
        }

        return new ReportDetailResponse(
            report.Id, report.PlantId, plantName,
            report.Type.ToString(), report.Status.ToString(), report.Format.ToString(),
            report.RangeStart, report.RangeEnd, report.FileContent,
            report.CreatedAt, report.GeneratedAt);
    }

    private static IReadOnlyList<TrendPoint> AggregateByDay(List<TelemetryData> readings, DateTime now)
    {
        var from = now.AddDays(-7);
        return readings
            .Where(r => r.RecordedAt >= from)
            .GroupBy(r => r.RecordedAt.Date)
            .OrderBy(g => g.Key)
            .Take(7)
            .Select(g => BuildTrendPoint(g.ToList(), g.Key.ToString("MMM dd")))
            .ToList();
    }

    private static IReadOnlyList<TrendPoint> AggregateByWeek(List<TelemetryData> readings, DateTime now)
    {
        var from = now.AddDays(-56);
        return readings
            .Where(r => r.RecordedAt >= from)
            .GroupBy(r => GetWeekStart(r.RecordedAt))
            .OrderBy(g => g.Key)
            .Take(8)
            .Select(g => BuildTrendPoint(g.ToList(), $"W{g.Key:MMM dd}"))
            .ToList();
    }

    private static IReadOnlyList<TrendPoint> AggregateByMonth(List<TelemetryData> readings, DateTime now)
    {
        var from = now.AddMonths(-6);
        return readings
            .Where(r => r.RecordedAt >= from)
            .GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, 1))
            .OrderBy(g => g.Key)
            .Take(6)
            .Select(g => BuildTrendPoint(g.ToList(), g.Key.ToString("MMM yyyy")))
            .ToList();
    }

    private static TrendPoint BuildTrendPoint(List<TelemetryData> group, string label)
    {
        return new TrendPoint(
            label,
            Math.Round(group.Average(r => r.HealthScore), 1),
            Math.Round(group.Average(r => r.SoilMoisture), 1),
            Math.Round(group.Average(r => r.Temperature), 1),
            Math.Round(group.Average(r => r.Humidity), 1),
            group.Count);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static string GenerateCsv(List<TelemetryData> readings)
    {
        var header = "RecordedAt,DeviceId,HealthScore,SoilMoisture,Temperature,Humidity,LightLevel";
        var rows = readings.Select(r =>
            $"{r.RecordedAt:O},{r.DeviceId},{r.HealthScore},{r.SoilMoisture},{r.Temperature},{r.Humidity},{r.LightLevel}");
        return string.Join(Environment.NewLine, new[] { header }.Concat(rows));
    }

    private static string GenerateJson(List<TelemetryData> readings)
    {
        var items = readings.Select(r =>
            $"{{\"recordedAt\":\"{r.RecordedAt:O}\",\"deviceId\":\"{r.DeviceId}\",\"healthScore\":{r.HealthScore},\"soilMoisture\":{r.SoilMoisture},\"temperature\":{r.Temperature},\"humidity\":{r.Humidity},\"lightLevel\":{r.LightLevel}}}");
        return $"[{string.Join(",", items)}]";
    }
}
