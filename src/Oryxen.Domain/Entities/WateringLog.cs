using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

/// <summary>Records a single watering event for a <see cref="Plant"/>.</summary>
public class WateringLog : AuditableEntity
{
    public Guid PlantId { get; set; }

    public DateTime WateredAt { get; set; } = DateTime.UtcNow;
}
