using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

/// <summary>
/// A single telemetry reading emitted by a Sensor Lite device for a given plant.
/// Belongs to the Device Management / Analysis &amp; Reporting bounded contexts.
/// </summary>
public class TelemetryData : AuditableEntity
{
    /// <summary>Serial / hardware identifier of the Sensor Lite that produced the reading.</summary>
    public string DeviceId { get; set; } = null!;

    public Guid PlantId { get; set; }

    public double Humidity { get; set; }

    public double Temperature { get; set; }

    public double LightLevel { get; set; }

    public double SoilMoisture { get; set; }

    /// <summary>Derived plant Health Score (0-100) computed at ingestion time.</summary>
    public int HealthScore { get; set; }

    /// <summary>Timestamp reported by the device (may differ from <see cref="AuditableEntity.CreatedAt"/>).</summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
