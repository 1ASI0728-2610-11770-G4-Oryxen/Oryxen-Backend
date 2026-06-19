using Oryxen.Domain.Common;
using Oryxen.Domain.Enums;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Billing &amp; Subscription entity tied one-to-one to a <see cref="UserAccount"/>.
/// New accounts are provisioned with an active Freemium plan.
/// </summary>
public class Subscription : AuditableEntity
{
    public Guid UserAccountId { get; set; }

    public UserAccount UserAccount { get; set; } = null!;

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Freemium;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}
