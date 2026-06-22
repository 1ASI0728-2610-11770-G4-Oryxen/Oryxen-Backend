using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

internal sealed class PlanRepository : IPlanRepository
{
    private readonly OryxenDbContext _db;

    public PlanRepository(OryxenDbContext db) => _db = db;

    public async Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Plans.OrderBy(p => p.Price).ToListAsync(cancellationToken);
    }

    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Plans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Plan?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _db.Plans.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default) =>
        await _db.Plans.AddAsync(plan, cancellationToken);
}
