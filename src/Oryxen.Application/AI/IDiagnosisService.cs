using Oryxen.Application.AI.Contracts;

namespace Oryxen.Application.AI;

/// <summary>
/// Application service for the Artificial Intelligence bounded context. Orchestrates the
/// multimodal diagnosis flow: validates the target plant, enriches the call with the latest
/// telemetry reading, invokes the AI provider and persists the resulting aggregate.
/// </summary>
public interface IDiagnosisService
{
    /// <summary>Creates a new diagnosis for a plant: upload image + latest telemetry → AI analysis.</summary>
    Task<DiagnosisResponse> CreateAsync(
        Guid userAccountId,
        Guid plantId,
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default);

    Task<DiagnosisResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagnosisResponse>> GetByPlantAsync(Guid plantId, CancellationToken cancellationToken = default);
}
