namespace Oryxen.Application.Auth.Contracts;

/// <summary>
/// Result returned by register / login / refresh. Carries the freshly issued token pair
/// plus a lightweight projection of the authenticated account.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);
