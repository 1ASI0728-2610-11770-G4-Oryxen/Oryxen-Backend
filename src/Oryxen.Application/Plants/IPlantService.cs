using Oryxen.Application.Plants.Contracts;
using Oryxen.Application.Weather.Contracts;

namespace Oryxen.Application.Plants;

/// <summary>Application service for the Plant Management bounded context.</summary>
public interface IPlantService
{
    Task<IReadOnlyList<PlantResponse>> GetByUserAsync(Guid userAccountId, CancellationToken cancellationToken = default);

    Task<PlantResponse> GetByIdAsync(Guid plantId, CancellationToken cancellationToken = default);

    Task<PlantResponse> CreateAsync(Guid ownerId, CreatePlantRequest request, CancellationToken cancellationToken = default);

    Task<PlantResponse> UpdateAsync(Guid plantId, UpdatePlantRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid plantId, CancellationToken cancellationToken = default);

    Task<PlantResponse> WaterAsync(Guid plantId, WaterPlantRequest request, CancellationToken cancellationToken = default);

    Task<PlantResponse> AssignSensorAsync(Guid plantId, AssignSensorRequest request, CancellationToken cancellationToken = default);

    Task<WeatherResponse> GetWeatherAsync(Guid plantId, CancellationToken cancellationToken = default);
}
