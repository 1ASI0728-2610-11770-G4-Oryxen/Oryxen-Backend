using Oryxen.Application.Common.Models;
using Oryxen.Domain.Entities;

namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Issues signed JWT access tokens and opaque refresh tokens. The concrete
/// implementation (Infrastructure layer) owns the signing key and token lifetimes.
/// </summary>
public interface IJwtTokenGenerator
{
    TokenResult GenerateAccessToken(UserAccount user, IEnumerable<string> roles);

    TokenResult GenerateRefreshToken();

    /// <summary>Deterministic SHA-256 hash used to persist/look-up refresh tokens at rest.</summary>
    string HashRefreshToken(string refreshToken);
}
