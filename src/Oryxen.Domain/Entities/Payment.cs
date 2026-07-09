using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Represents a payment transaction processed by an external payment platform (Stripe).
/// Linked to a <see cref="Subscription"/> and tracked for audit and reconciliation.
/// </summary>
public class Payment : AuditableEntity
{
    public Guid SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Name of the payment platform (e.g. "Stripe").</summary>
    public string Provider { get; set; } = "Stripe";

    /// <summary>Transaction/session ID returned by the payment platform.</summary>
    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }
}

/// <summary>Lifecycle status of a payment transaction.</summary>
public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4
}
