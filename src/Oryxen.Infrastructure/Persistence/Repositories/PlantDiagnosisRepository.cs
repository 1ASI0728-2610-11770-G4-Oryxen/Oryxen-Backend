using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPlantDiagnosisRepository"/>. Diagnoses are
/// returned newest-first to match the convention used by the telemetry repository.
/// </summary>
internal sealed class PlantDiagnosisRepository : IPlantDiagnosisRepository
{
    private readonly OryxenDbContext _db;

    public PlantDiagnosisRepository(OryxenDbContext db) => _db = db;

    public Task<PlantDiagnosis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Diagnoses.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PlantDiagnosis>> GetByPlantAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.Diagnoses
            .Where(d => d.PlantId == plantId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task AddAsync(PlantDiagnosis diagnosis, CancellationToken cancellationToken = default) =>
        await _db.Diagnoses.AddAsync(diagnosis, cancellationToken);
}
