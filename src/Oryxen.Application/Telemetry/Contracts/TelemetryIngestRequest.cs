using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Telemetry.Contracts;

/// <summary>Payload pushed by a Sensor Lite device when reporting a new reading.</summary>
public sealed record TelemetryIngestRequest
{
    [Required]
    [MaxLength(64)]
    public string DeviceId { get; init; } = null!;

    [Required]
    public Guid PlantId { get; init; }

    [Range(0, 100)]
    public double Humidity { get; init; }

    [Range(-40, 85)]
    public double Temperature { get; init; }

    [Range(0, 100000)]
    public double LightLevel { get; init; }

    [Range(0, 100)]
    public double SoilMoisture { get; init; }

    /// <summary>Optional device timestamp; defaults to server UTC time when omitted.</summary>
    public DateTime? RecordedAt { get; init; }
}
