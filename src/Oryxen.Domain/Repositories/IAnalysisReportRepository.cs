using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

public interface IAnalysisReportRepository
{
    Task AddAsync(AnalysisReport report, CancellationToken cancellationToken = default);

    Task<AnalysisReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalysisReport>> GetByUserAsync(
        Guid userAccountId,
        Guid? plantId = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(
        Guid userAccountId,
        Guid? plantId = null,
        CancellationToken cancellationToken = default);
}
