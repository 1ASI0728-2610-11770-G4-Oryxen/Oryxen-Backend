namespace Oryxen.Domain.Enums;

/// <summary>
/// Current state of a user's <see cref="Entities.Subscription"/>.
/// </summary>
public enum SubscriptionStatus
{
    Active = 1,
    Cancelled = 2,
    Expired = 3
}
