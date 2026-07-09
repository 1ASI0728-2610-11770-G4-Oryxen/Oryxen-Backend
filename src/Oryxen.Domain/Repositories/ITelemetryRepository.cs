using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

public interface ITelemetryRepository
{
    Task AddAsync(TelemetryData reading, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryData>> GetByPlantAsync(
        Guid plantId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TelemetryData> readings, CancellationToken cancellationToken = default);

    Task<TelemetryData?> GetLatestByPlantAsync(Guid plantId, CancellationToken cancellationToken = default);
}
