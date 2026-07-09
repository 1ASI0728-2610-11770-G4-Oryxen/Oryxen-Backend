using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="Plan"/> entity. Plans are seeded and
/// mostly read-only; the repository exposes query operations for the catalog.
/// </summary>
public interface IPlanRepository
{
    Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Plan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
}
