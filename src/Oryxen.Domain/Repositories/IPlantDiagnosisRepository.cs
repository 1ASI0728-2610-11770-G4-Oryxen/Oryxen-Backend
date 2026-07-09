using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

/// <summary>
/// Persistence contract for the AI bounded context's <see cref="PlantDiagnosis"/> aggregate.
/// Implemented by EF Core in the Infrastructure layer.
/// </summary>
public interface IPlantDiagnosisRepository
{
    Task<PlantDiagnosis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlantDiagnosis>> GetByPlantAsync(Guid plantId, CancellationToken cancellationToken = default);

    Task AddAsync(PlantDiagnosis diagnosis, CancellationToken cancellationToken = default);
}
