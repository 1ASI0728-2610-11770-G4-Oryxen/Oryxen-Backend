using Oryxen.Domain.Common;
using Oryxen.Domain.Enums;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Billing &amp; Subscription entity tied one-to-one to a <see cref="UserAccount"/>.
/// New accounts are provisioned with an active Freemium plan. Upgrades to Premium are
/// processed via Stripe Checkout Sessions and webhook events.
/// </summary>
public class Subscription : AuditableEntity
{
    public Guid UserAccountId { get; set; }

    public UserAccount UserAccount { get; set; } = null!;

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Freemium;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    /// <summary>Next automated billing date (set when a Premium plan is active).</summary>
    public DateTime? NextBillingDate { get; set; }

    /// <summary>UTC instant when the subscription was cancelled, if applicable.</summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>Stripe Customer ID linked to this subscription's owner.</summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>Stripe Subscription ID returned by the webhook after checkout completion.</summary>
    public string? StripeSubscriptionId { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
