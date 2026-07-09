using Oryxen.Application.Billing.Contracts;

namespace Oryxen.Application.Billing;

/// <summary>
/// Application service for subscription management: creates checkout sessions via the
/// payment platform, processes webhook events to activate/renew plans, and exposes the
/// current user's subscription status.
/// </summary>
public interface ISubscriptionService
{
    Task<CheckoutResponse> CreateCheckoutAsync(Guid userAccountId, CheckoutRequest request, CancellationToken cancellationToken = default);

    Task ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> GetCurrentAsync(Guid userAccountId, CancellationToken cancellationToken = default);
}
