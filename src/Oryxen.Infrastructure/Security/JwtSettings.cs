namespace Oryxen.Infrastructure.Security;

/// <summary>Strongly-typed binding for the "Jwt" configuration section.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    /// <summary>Symmetric signing key (HS256). Must be at least 32 bytes long.</summary>
    public string Secret { get; set; } = null!;

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 7;
}
