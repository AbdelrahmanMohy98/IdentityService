using IdentityService.Domain.Common;

namespace IdentityService.Domain.Users;

// Entity (not a value object) because it has identity and a mutable lifecycle
// (revoked, replaced) that must be tracked independently of its value.
public sealed class RefreshToken : Entity<Guid>
{
    public string TokenHash { get; private set; } = default!;
    public UserId UserId { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresOnUtc;
    public bool IsRevoked => RevokedOnUtc is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { }

    public static RefreshToken Issue(UserId userId, string tokenHash, TimeSpan lifetime, string? createdByIp)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedOnUtc = DateTime.UtcNow,
            ExpiresOnUtc = DateTime.UtcNow.Add(lifetime),
            CreatedByIp = createdByIp
        };
    }

    public void Revoke(Guid? replacedByTokenId = null)
    {
        if (IsRevoked) return;
        RevokedOnUtc = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
