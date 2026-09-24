using IdentityService.Domain.Users;

namespace IdentityService.Application.Common.Interfaces;

public sealed record AccessToken(string Value, DateTime ExpiresOnUtc);

public interface IJwtTokenGenerator
{
    AccessToken GenerateAccessToken(User user);

    // Returned as plaintext to hand to the client; only its hash is persisted
    // (see IPasswordHasher-style hashing in Infrastructure) so a leaked DB
    // backup can't be used to mint sessions.
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
