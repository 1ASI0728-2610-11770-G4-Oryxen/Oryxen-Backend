namespace Oryxen.Application.Common.Exceptions;

/// <summary>Thrown when a <see cref="Domain.Entities.Plan"/> lookup misses. Maps to HTTP 404.</summary>
public sealed class PlanNotFoundException : Exception
{
    public PlanNotFoundException(Guid id)
        : base($"Plan '{id}' was not found.")
    {
    }

    public PlanNotFoundException(string message)
        : base(message)
    {
    }
}
