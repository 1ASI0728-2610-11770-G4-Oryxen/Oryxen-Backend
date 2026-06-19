using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

public sealed class TelemetryRepository : ITelemetryRepository
{
    private const int MaxRows = 500;

    private readonly OryxenDbContext _db;

    public TelemetryRepository(OryxenDbContext db) => _db = db;

    public async Task AddAsync(TelemetryData reading, CancellationToken cancellationToken = default) =>
        await _db.TelemetryReadings.AddAsync(reading, cancellationToken);

    public async Task<IReadOnlyList<TelemetryData>> GetByPlantAsync(
        Guid plantId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TelemetryReadings
            .AsNoTracking()
            .Where(t => t.PlantId == plantId);

        if (from.HasValue)
        {
            query = query.Where(t => t.RecordedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.RecordedAt <= to.Value);
        }

        return await query
            .OrderByDescending(t => t.RecordedAt)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);
    }
}
