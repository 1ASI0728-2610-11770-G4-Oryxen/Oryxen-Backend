namespace Oryxen.Application.AI.Contracts;

/// <summary>
/// Projection of a <c>PlantDiagnosis</c> returned to the web/mobile clients. Property
/// names serialize (camelCase) exactly as the TypeScript <c>Diagnosis</c> interface expects.
/// </summary>
public sealed record DiagnosisResponse(
    Guid Id,
    Guid PlantId,
    string ImageUrl,
    string DetectedPest,
    double ConfidenceScore,
    string Recommendation,
    string Status,
    DateTime CreatedAt,
    DateTime? AnalyzedAt);
