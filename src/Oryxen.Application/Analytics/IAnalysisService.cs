using Oryxen.Application.Analytics.Contracts;

namespace Oryxen.Application.Analytics;

public interface IAnalysisService
{
    Task<DashboardResponse> GetDashboardAsync(Guid userAccountId, CancellationToken cancellationToken = default);

    Task<PlantTrendResponse> GetPlantTrendsAsync(Guid plantId, CancellationToken cancellationToken = default);

    Task<ReportListResponse> GetReportsAsync(
        Guid userAccountId,
        Guid? plantId = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default);

    Task<ReportDetailResponse> GenerateReportAsync(
        Guid userAccountId,
        GenerateReportRequest request,
        CancellationToken cancellationToken = default);

    Task<ReportDetailResponse?> GetReportByIdAsync(Guid reportId, CancellationToken cancellationToken = default);
}
