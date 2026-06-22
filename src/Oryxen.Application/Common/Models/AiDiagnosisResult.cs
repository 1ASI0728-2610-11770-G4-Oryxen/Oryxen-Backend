namespace Oryxen.Application.Common.Models;

/// <summary>
/// Normalized result returned by the multimodal AI provider (Gemini Vision API). The
/// Application layer maps this into a <see cref="AI.Contracts.DiagnosisResponse"/> and
/// persists it inside a <c>PlantDiagnosis</c> aggregate.
/// </summary>
public sealed record AiDiagnosisResult(
    string DetectedPest,
    double ConfidenceScore,
    string Recommendation);
