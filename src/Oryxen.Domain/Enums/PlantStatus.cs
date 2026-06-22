namespace Oryxen.Domain.Enums;

/// <summary>
/// Health classification of a plant, derived from the latest Sensor Lite Health Score.
/// Persisted as a string and surfaced (lower-cased) to the web/mobile clients.
/// </summary>
public enum PlantStatus
{
    Healthy,
    Warning,
    Critical
}
