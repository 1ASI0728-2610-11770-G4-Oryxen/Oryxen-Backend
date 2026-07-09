namespace Oryxen.Application.Common.Exceptions;

/// <summary>
/// Thrown when a payment-platform webhook delivery carries a missing, expired or forged
/// signature. Mapped to HTTP 400 so the caller knows the request was rejected as invalid.
/// </summary>
public sealed class InvalidWebhookSignatureException : Exception
{
    public InvalidWebhookSignatureException()
        : base("Webhook signature verification failed.")
    {
    }
}
