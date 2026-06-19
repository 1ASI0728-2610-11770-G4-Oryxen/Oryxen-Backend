namespace Oryxen.Application.Common.Models;

/// <summary>
/// A generated token together with its absolute expiration instant (UTC).
/// Used for both access tokens and refresh tokens.
/// </summary>
public sealed record TokenResult(string Token, DateTime ExpiresAt);
