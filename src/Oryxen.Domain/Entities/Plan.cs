using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Commercial plan offered by the Oryxen Billing &amp; Subscription model. Each plan defines
/// a price, currency, billing cycle and a set of features. Plans are seeded at deployment
/// time and referenced by <see cref="Subscription"/> aggregates.
/// </summary>
public class Plan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    /// <summary>Billing cycle in months (1 = monthly, 12 = annual).</summary>
    public int BillingCycleMonths { get; set; } = 1;

    /// <summary>Comma-separated list of feature descriptions for display.</summary>
    public string Features { get; set; } = string.Empty;

    /// <summary>Stripe Price ID linked to this plan (for checkout sessions).</summary>
    public string? StripePriceId { get; set; }

    public bool IsActive { get; set; } = true;
}
