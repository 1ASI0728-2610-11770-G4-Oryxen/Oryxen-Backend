namespace Oryxen.Application.Common.Exceptions;

/// <summary>Thrown when a supplied refresh token is unknown, revoked or expired. Maps to HTTP 401.</summary>
public sealed class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid or has expired.")
    {
    }
}
