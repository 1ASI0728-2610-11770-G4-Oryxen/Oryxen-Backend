using Oryxen.Domain.Common;
using Oryxen.Domain.Enums;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Aggregate root of the Plant Management bounded context: a plant owned by a
/// <see cref="UserAccount"/>, optionally bound to a Sensor Lite device and tracked
/// through watering logs and derived telemetry health.
/// </summary>
public class Plant : AuditableEntity
{
    public Guid UserAccountId { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string ImgUrl { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public PlantStatus Status { get; set; } = PlantStatus.Healthy;

    /// <summary>Serial of the Sensor Lite assigned to this plant; null when none is bound.</summary>
    public string? AssignedDeviceId { get; set; }

    public DateTime? LastWateredAt { get; set; }

    public DateTime? NextWateringAt { get; set; }

    public ICollection<WateringLog> WateringLogs { get; set; } = new List<WateringLog>();
}
