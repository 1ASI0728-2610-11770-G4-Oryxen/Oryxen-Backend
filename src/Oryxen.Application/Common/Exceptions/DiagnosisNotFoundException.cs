namespace Oryxen.Application.Common.Exceptions;

/// <summary>Thrown when a <see cref="Domain.Entities.PlantDiagnosis"/> lookup misses. Maps to HTTP 404.</summary>
public sealed class DiagnosisNotFoundException : Exception
{
    public DiagnosisNotFoundException(Guid id)
        : base($"Plant diagnosis '{id}' was not found.")
    {
    }
}
