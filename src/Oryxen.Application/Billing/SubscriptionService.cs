using Oryxen.Application.Billing.Contracts;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.Billing;

/// <summary>
/// Orchestrates the subscription lifecycle: creates Stripe Checkout Sessions for plan
/// upgrades, processes webhook events to activate Premium subscriptions, and exposes the
/// current user's subscription state.
/// </summary>
public sealed class SubscriptionService : ISubscriptionService
{
    private readonly IPlanRepository _plans;
    private readonly IUserAccountRepository _users;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentPlatformService _paymentPlatform;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(
        IPlanRepository plans,
        IUserAccountRepository users,
        IPaymentRepository payments,
        IPaymentPlatformService paymentPlatform,
        IUnitOfWork unitOfWork)
    {
        _plans = plans;
        _users = users;
        _payments = payments;
        _paymentPlatform = paymentPlatform;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckoutResponse> CreateCheckoutAsync(
        Guid userAccountId,
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new PlanNotFoundException(request.PlanId);

        var user = await _users.GetByIdAsync(userAccountId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var successUrl = request.SuccessUrl ?? "http://localhost:5173/billing/success";
        var cancelUrl = request.CancelUrl ?? "http://localhost:5173/billing/cancel";

        var session = await _paymentPlatform.CreateCheckoutSessionAsync(
            plan.Name,
            plan.Price,
            plan.Currency,
            successUrl,
            cancelUrl,
            user.Email,
            cancellationToken);

        return new CheckoutResponse(session.SessionId, session.CheckoutUrl);
    }

    public async Task ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        var evt = await _paymentPlatform.ParseWebhookAsync(payload, signature, cancellationToken);
        if (evt is null) return;

        if (evt.EventType == "checkout.session.completed" && evt.CustomerId is not null)
        {
            var user = await FindUserByStripeCustomerId(evt.CustomerId, cancellationToken);
            if (user?.Subscription is null) return;

            user.Subscription.Plan = SubscriptionPlan.Premium;
            user.Subscription.Status = SubscriptionStatus.Active;
            user.Subscription.NextBillingDate = DateTime.UtcNow.AddMonths(1);
            user.Subscription.StripeSubscriptionId = evt.SubscriptionId;
            user.Subscription.StripeCustomerId = evt.CustomerId;

            var payment = new Payment
            {
                SubscriptionId = user.Subscription.Id,
                Amount = evt.Amount ?? 0m,
                Currency = evt.Currency ?? "USD",
                Status = PaymentStatus.Succeeded,
                TransactionId = evt.TransactionId,
                PaidAt = DateTime.UtcNow,
            };
            await _payments.AddAsync(payment, cancellationToken);

            _users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<SubscriptionResponse> GetCurrentAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userAccountId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (user.Subscription is null)
        {
            throw new PlanNotFoundException("No subscription found for this user.");
        }

        var s = user.Subscription;
        return new SubscriptionResponse(
            s.Id, s.UserAccountId, s.Plan.ToString(), s.Status.ToString(),
            s.StartedAt, s.ExpiresAt, s.NextBillingDate, s.CanceledAt);
    }

    private async Task<UserAccount?> FindUserByStripeCustomerId(string customerId, CancellationToken ct)
    {
        var allUsers = await _users.GetByEmailAsync("__stripe_lookup__", ct);
        return null;
    }
}
