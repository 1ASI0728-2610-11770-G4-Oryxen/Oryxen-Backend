namespace Oryxen.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.PlantDiagnosis"/>: the AI analysis may be
/// in-flight, completed successfully, or failed because the multimodal service was
/// unreachable or returned an unparseable payload.
/// </summary>
public enum DiagnosisStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
