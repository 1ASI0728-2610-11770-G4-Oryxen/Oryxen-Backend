namespace Oryxen.Application.Common.Exceptions;

/// <summary>Thrown when a plant cannot be located by its identifier. Maps to HTTP 404.</summary>
public sealed class PlantNotFoundException : Exception
{
    public PlantNotFoundException(Guid plantId)
        : base($"Plant '{plantId}' was not found.")
    {
    }
}
