namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Port for the external payment platform (Stripe). The Application layer uses this
/// abstraction to create checkout sessions and process webhook events without coupling
/// to the Stripe SDK directly. Implemented by <c>StripePaymentService</c> in Infrastructure.
/// </summary>
public interface IPaymentPlatformService
{
    /// <summary>Creates a Stripe Checkout Session URL for the given plan and customer.</summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        string planName,
        decimal amount,
        string currency,
        string successUrl,
        string cancelUrl,
        string customerEmail,
        string clientReferenceId,
        CancellationToken cancellationToken = default);

    /// <summary>Verifies and parses a Stripe webhook payload. Returns the event type or null if invalid.</summary>
    Task<WebhookEventResult?> ParseWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of creating a checkout session.</summary>
public sealed record CheckoutSessionResult(
    string SessionId,
    string CheckoutUrl);

/// <summary>Result of parsing a webhook event.</summary>
public sealed record WebhookEventResult(
    string EventType,
    string? SubscriptionId,
    string? CustomerId,
    string? ClientReferenceId,
    string? TransactionId,
    decimal? Amount,
    string? Currency);
