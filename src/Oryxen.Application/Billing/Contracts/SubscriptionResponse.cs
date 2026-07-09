namespace Oryxen.Application.Billing.Contracts;

/// <summary>Projection of the authenticated user's subscription.</summary>
public sealed record SubscriptionResponse(
    Guid Id,
    Guid UserId,
    string Plan,
    string Status,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    DateTime? NextBillingDate,
    DateTime? CanceledAt);
