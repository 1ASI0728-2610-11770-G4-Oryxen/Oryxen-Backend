namespace Oryxen.Application.Common.Exceptions;

/// <summary>Thrown when authentication fails due to wrong credentials or an inactive account. Maps to HTTP 401.</summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
