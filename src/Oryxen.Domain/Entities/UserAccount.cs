using Oryxen.Domain.Common;
using Oryxen.Domain.Enums;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Aggregate root of the Auth &amp; Identity bounded context. Represents an
/// authenticated principal of the Oryxen platform together with its roles,
/// subscription and the hashed refresh-token currently issued to it.
/// </summary>
public class UserAccount : AuditableEntity
{
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    /// <summary>SHA-256 hash of the active refresh token. Null when no token is issued.</summary>
    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public ICollection<Role> Roles { get; set; } = new List<Role>();

    public Subscription? Subscription { get; set; }

    public bool IsActive => Status == AccountStatus.Active;

    public void SetRefreshToken(string tokenHash, DateTime expiresAt)
    {
        RefreshTokenHash = tokenHash;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsRefreshTokenValid(string tokenHash)
    {
        return RefreshTokenHash is not null
            && RefreshTokenHash == tokenHash
            && RefreshTokenExpiresAt.HasValue
            && RefreshTokenExpiresAt.Value > DateTime.UtcNow;
    }
}
