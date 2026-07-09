using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Common.Models;
using Oryxen.Application.AI.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.AI;

/// <summary>
/// Application service that orchestrates the multimodal AI diagnosis flow:
/// 1. Verifies the plant exists and belongs to the requesting user.
/// 2. Pulls the latest telemetry reading to enrich the prompt (multimodal context).
/// 3. Calls <see cref="IMultimodalAiService"/> (Gemini Vision) with image + telemetry.
/// 4. Persists the resulting <see cref="PlantDiagnosis"/> aggregate.
/// </summary>
public sealed class DiagnosisService : IDiagnosisService
{
    private readonly IPlantRepository _plants;
    private readonly IPlantDiagnosisRepository _diagnoses;
    private readonly ITelemetryRepository _telemetry;
    private readonly IMultimodalAiService _ai;
    private readonly IUnitOfWork _unitOfWork;

    public DiagnosisService(
        IPlantRepository plants,
        IPlantDiagnosisRepository diagnoses,
        ITelemetryRepository telemetry,
        IMultimodalAiService ai,
        IUnitOfWork unitOfWork)
    {
        _plants = plants;
        _diagnoses = diagnoses;
        _telemetry = telemetry;
        _ai = ai;
        _unitOfWork = unitOfWork;
    }

    public async Task<DiagnosisResponse> CreateAsync(
        Guid userAccountId,
        Guid plantId,
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var plant = await _plants.GetByIdAsync(plantId, cancellationToken)
            ?? throw new PlantNotFoundException(plantId);

        if (plant.UserAccountId != userAccountId)
        {
            throw new PlantNotFoundException(plantId);
        }

        var readings = await _telemetry.GetByPlantAsync(plantId, from: null, to: null, cancellationToken);
        var latest = readings.Count > 0 ? readings[0] : null;

        var diagnosis = new PlantDiagnosis
        {
            PlantId = plantId,
            ImageUrl = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}",
            Status = DiagnosisStatus.Pending
        };

        await _diagnoses.AddAsync(diagnosis, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await _ai.AnalyzeAsync(
                imageBytes,
                mimeType,
                latest?.SoilMoisture,
                latest?.Humidity,
                latest?.Temperature,
                cancellationToken);

            diagnosis.DetectedPest = result.DetectedPest;
            diagnosis.ConfidenceScore = result.ConfidenceScore;
            diagnosis.Recommendation = result.Recommendation;
            diagnosis.Status = DiagnosisStatus.Completed;
            diagnosis.AnalyzedAt = DateTime.UtcNow;
        }
        catch (ExternalServiceException)
        {
            diagnosis.Status = DiagnosisStatus.Failed;
            throw;
        }
        finally
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(diagnosis);
    }

    public async Task<DiagnosisResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var diagnosis = await _diagnoses.GetByIdAsync(id, cancellationToken)
            ?? throw new DiagnosisNotFoundException(id);

        return ToResponse(diagnosis);
    }

    public async Task<IReadOnlyList<DiagnosisResponse>> GetByPlantAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var diagnoses = await _diagnoses.GetByPlantAsync(plantId, cancellationToken);
        return diagnoses.Select(ToResponse).ToArray();
    }

    private static DiagnosisResponse ToResponse(PlantDiagnosis d) =>
        new(
            d.Id,
            d.PlantId,
            d.ImageUrl,
            d.DetectedPest,
            d.ConfidenceScore,
            d.Recommendation,
            d.Status.ToString(),
            d.CreatedAt,
            d.AnalyzedAt);
}
