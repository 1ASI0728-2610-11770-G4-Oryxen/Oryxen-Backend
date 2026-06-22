using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

public sealed class PlantRepository : IPlantRepository
{
    private readonly OryxenDbContext _db;

    public PlantRepository(OryxenDbContext db) => _db = db;

    public async Task<Plant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Plants
            .Include(p => p.WateringLogs)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Plant>> GetByUserAsync(Guid userAccountId, CancellationToken cancellationToken = default) =>
        await _db.Plants
            .Include(p => p.WateringLogs)
            .Where(p => p.UserAccountId == userAccountId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Plant plant, CancellationToken cancellationToken = default) =>
        await _db.Plants.AddAsync(plant, cancellationToken);

    public void Remove(Plant plant) => _db.Plants.Remove(plant);
}
