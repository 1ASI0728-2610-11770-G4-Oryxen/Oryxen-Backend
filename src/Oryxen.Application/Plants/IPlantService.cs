using Oryxen.Application.Plants.Contracts;
using Oryxen.Application.Weather.Contracts;

namespace Oryxen.Application.Plants;

/// <summary>
/// Application service for the Plant Management bounded context. Per-plant operations
/// receive the requester's identity so the service can enforce object-level authorization:
/// FARMERs may only operate on their own plants, ADMINs may operate on any plant.
/// </summary>
public interface IPlantService
{
    Task<IReadOnlyList<PlantResponse>> GetByUserAsync(Guid userAccountId, CancellationToken cancellationToken = default);

    Task<PlantResponse> GetByIdAsync(Guid plantId, Guid requesterId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<PlantResponse> CreateAsync(Guid ownerId, CreatePlantRequest request, CancellationToken cancellationToken = default);

    Task<PlantResponse> UpdateAsync(Guid plantId, Guid requesterId, bool isAdmin, UpdatePlantRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid plantId, Guid requesterId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<PlantResponse> WaterAsync(Guid plantId, Guid requesterId, bool isAdmin, WaterPlantRequest request, CancellationToken cancellationToken = default);

    Task<PlantResponse> AssignSensorAsync(Guid plantId, Guid requesterId, bool isAdmin, AssignSensorRequest request, CancellationToken cancellationToken = default);

    Task<WeatherResponse> GetWeatherAsync(Guid plantId, Guid requesterId, bool isAdmin, CancellationToken cancellationToken = default);
}
