using IdentityService.Domain.Common;

namespace IdentityService.Domain.Users;

// Wraps an already-hashed value. Hashing itself is an infrastructure concern
// (BCrypt/Argon2 belong in Infrastructure.Security.IPasswordHasher) — the
// domain only ever sees and stores the hash, never a plaintext password.
public sealed class HashedPassword : ValueObject
{
    public string Value { get; }

    private HashedPassword(string value) => Value = value;

    public static HashedPassword FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainException("Password hash cannot be empty.");

        return new HashedPassword(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
