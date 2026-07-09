namespace Oryxen.Infrastructure.External;

/// <summary>
/// Strongly-typed binding for the "Stripe" configuration section. The secret key is
/// injected via the <c>Stripe__SecretKey</c> environment variable and the webhook
/// endpoint signing secret via <c>Stripe__WebhookSecret</c>.
/// </summary>
public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.stripe.com";
}
