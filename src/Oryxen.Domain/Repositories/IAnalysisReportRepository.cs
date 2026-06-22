using Oryxen.Domain.Entities;
using Oryxen.Domain.ValueObjects;

namespace Oryxen.Domain.Repositories;

public interface IAnalysisReportRepository
{
    Task AddAsync(AnalysisReport report, CancellationToken cancellationToken = default);

    Task<AnalysisReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportProjection>> GetReportProjectionsAsync(
        Guid userAccountId,
        Guid? plantId = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(
        Guid userAccountId,
        Guid? plantId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlantMetricProjection>> GetDashboardMetricsAsync(
        Guid userAccountId,
        DateTime since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendPointProjection>> GetDailyTrendsAsync(
        Guid plantId,
        DateTime since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendPointProjection>> GetWeeklyTrendsAsync(
        Guid plantId,
        DateTime since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendPointProjection>> GetMonthlyTrendsAsync(
        Guid plantId,
        DateTime since,
        CancellationToken cancellationToken = default);
}
