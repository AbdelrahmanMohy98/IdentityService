using IdentityService.Application.Common.Interfaces;

namespace IdentityService.Infrastructure.Security;

// BCrypt with a per-call work factor (12) is adequate for a service that
// hashes on login/registration only, not in a hot path. Swap for Argon2id
// via a second implementation of IPasswordHasher if the threat model needs it.
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
}
