using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

public interface IPlantRepository
{
    Task<Plant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plant>> GetByUserAsync(Guid userAccountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plant>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Plant plant, CancellationToken cancellationToken = default);

    void Remove(Plant plant);
}
