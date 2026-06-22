namespace Oryxen.Application.Common.Exceptions;

/// <summary>
/// Thrown when an upstream third-party service (e.g. OpenWeatherMap) fails, is
/// unreachable or is misconfigured. Maps to HTTP 502 Bad Gateway.
/// </summary>
public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException(string message)
        : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
