using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.Security;

/// <summary>BCrypt-based implementation of <see cref="IPasswordHasher"/> (work factor 12).</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
