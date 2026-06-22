using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

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

    public async Task<IReadOnlyList<AnalysisReport>> GetByUserAsync(
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
        {
            query = query.Where(r => r.PlantId == plantId.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
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
        {
            query = query.Where(r => r.PlantId == plantId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
