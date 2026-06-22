namespace Oryxen.Application.Plants.Contracts;

/// <summary>Payload for a watering event; <see cref="WateredAt"/> defaults to server UTC when omitted.</summary>
public sealed record WaterPlantRequest
{
    public DateTime? WateredAt { get; init; }
}
