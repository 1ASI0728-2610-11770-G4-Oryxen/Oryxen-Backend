using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;
using Oryxen.Domain.ValueObjects;

namespace Oryxen.Infrastructure.Persistence.Repositories;

public sealed class AnalysisReportRepository : IAnalysisReportRepository
{
    private readonly OryxenDbContext _db;

    public AnalysisReportRepository(OryxenDbContext db) => _db = db;

    public async Task AddAsync(AnalysisReport report, CancellationToken cancellationToken = default) =>
        await _db.AnalysisReports.AddAsync(report, cancellationToken);

    public async Task<AnalysisReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        await _db.AnalysisReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

    public async Task<IReadOnlyList<ReportProjection>> GetReportProjectionsAsync(
        Guid userAccountId,
        Guid? plantId = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AnalysisReports
            .AsNoTracking()
            .Where(r => r.UserAccountId == userAccountId);

        if (plantId.HasValue)
            query = query.Where(r => r.PlantId == plantId.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(r => new ReportProjection(
                r.Id, r.PlantId, r.Type.ToString(), r.Status.ToString(),
                r.Format.ToString(), r.RangeStart, r.RangeEnd,
                r.CreatedAt, r.GeneratedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByUserAsync(
        Guid userAccountId,
        Guid? plantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AnalysisReports
            .AsNoTracking()
            .Where(r => r.UserAccountId == userAccountId);

        if (plantId.HasValue)
            query = query.Where(r => r.PlantId == plantId.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlantMetricProjection>> GetDashboardMetricsAsync(
        Guid userAccountId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var plants = await _db.Plants
            .AsNoTracking()
            .Where(p => p.UserAccountId == userAccountId)
            .ToListAsync(cancellationToken);

        var results = new List<PlantMetricProjection>();

        foreach (var plant in plants)
        {
            var readings = await _db.TelemetryReadings
                .AsNoTracking()
                .Where(t => t.PlantId == plant.Id && t.RecordedAt >= since)
                .ToListAsync(cancellationToken);

            if (readings.Count == 0)
            {
                results.Add(new PlantMetricProjection(
                    plant.Id, plant.Name, plant.Type,
                    plant.Status.ToString().ToLowerInvariant(),
                    0, 0, 0, 0, 0, 0, null));
                continue;
            }

            results.Add(new PlantMetricProjection(
                plant.Id, plant.Name, plant.Type,
                plant.Status.ToString().ToLowerInvariant(),
                Math.Round(readings.Average(r => r.HealthScore), 1),
                Math.Round(readings.Average(r => r.SoilMoisture), 1),
                Math.Round(readings.Average(r => r.Temperature), 1),
                Math.Round(readings.Average(r => r.Humidity), 1),
                Math.Round(readings.Average(r => r.LightLevel), 1),
                readings.Count,
                readings.Max(r => r.RecordedAt)));
        }

        return results;
    }

    public async Task<IReadOnlyList<TrendPointProjection>> GetDailyTrendsAsync(
        Guid plantId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var readings = await _db.TelemetryReadings
            .AsNoTracking()
            .Where(t => t.PlantId == plantId && t.RecordedAt >= since)
            .OrderBy(t => t.RecordedAt)
            .ToListAsync(cancellationToken);

        return readings
            .GroupBy(r => r.RecordedAt.Date)
            .OrderBy(g => g.Key)
            .Take(7)
            .Select(g => BuildTrend(g.ToList(), g.Key.ToString("MMM dd")))
            .ToList();
    }

    public async Task<IReadOnlyList<TrendPointProjection>> GetWeeklyTrendsAsync(
        Guid plantId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var readings = await _db.TelemetryReadings
            .AsNoTracking()
            .Where(t => t.PlantId == plantId && t.RecordedAt >= since)
            .OrderBy(t => t.RecordedAt)
            .ToListAsync(cancellationToken);

        return readings
            .GroupBy(r => GetWeekStart(r.RecordedAt))
            .OrderBy(g => g.Key)
            .Take(8)
            .Select(g => BuildTrend(g.ToList(), $"W{g.Key:MMM dd}"))
            .ToList();
    }

    public async Task<IReadOnlyList<TrendPointProjection>> GetMonthlyTrendsAsync(
        Guid plantId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var readings = await _db.TelemetryReadings
            .AsNoTracking()
            .Where(t => t.PlantId == plantId && t.RecordedAt >= since)
            .OrderBy(t => t.RecordedAt)
            .ToListAsync(cancellationToken);

        return readings
            .GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, 1))
            .OrderBy(g => g.Key)
            .Take(6)
            .Select(g => BuildTrend(g.ToList(), g.Key.ToString("MMM yyyy")))
            .ToList();
    }

    private static TrendPointProjection BuildTrend(List<TelemetryData> group, string label)
    {
        return new TrendPointProjection(
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
}
