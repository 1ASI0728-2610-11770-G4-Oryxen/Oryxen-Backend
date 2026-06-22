using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Plants.Contracts;

/// <summary>Binds a Sensor Lite device (by its hardware serial) to a plant.</summary>
public sealed record AssignSensorRequest
{
    [Required]
    [MaxLength(64)]
    public string DeviceId { get; init; } = null!;
}
