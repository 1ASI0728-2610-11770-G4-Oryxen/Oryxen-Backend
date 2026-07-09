namespace Oryxen.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user attempts to operate on a plant they do not own
/// (and they are not an ADMIN). Mapped to HTTP 403 by the exception middleware.
/// </summary>
public sealed class PlantAccessDeniedException : Exception
{
    public PlantAccessDeniedException(Guid plantId)
        : base($"You do not have access to plant '{plantId}'.")
    {
    }
}
