namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies user passwords. Implemented with BCrypt in the Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
