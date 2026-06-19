namespace Oryxen.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.UserAccount"/>.
/// </summary>
public enum AccountStatus
{
    Active = 1,
    Suspended = 2,
    PendingVerification = 3
}
