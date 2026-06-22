namespace Oryxen.Application.Billing.Contracts;

/// <summary>Request to start a checkout session for upgrading to a plan.</summary>
public sealed record CheckoutRequest
{
    public Guid PlanId { get; init; }
    public string? SuccessUrl { get; init; }
    public string? CancelUrl { get; init; }
}

/// <summary>Result of creating a checkout session: the URL the client should redirect to.</summary>
public sealed record CheckoutResponse(
    string SessionId,
    string CheckoutUrl);
