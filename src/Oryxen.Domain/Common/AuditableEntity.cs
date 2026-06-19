namespace Oryxen.Domain.Common;

/// <summary>
/// Base class for domain entities that require identity and audit timestamps.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
