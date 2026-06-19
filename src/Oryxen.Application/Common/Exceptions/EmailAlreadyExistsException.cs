namespace Oryxen.Application.Common.Exceptions;

/// <summary>Thrown when registering an account whose email is already in use. Maps to HTTP 409.</summary>
public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"An account with email '{email}' already exists.")
    {
    }
}
