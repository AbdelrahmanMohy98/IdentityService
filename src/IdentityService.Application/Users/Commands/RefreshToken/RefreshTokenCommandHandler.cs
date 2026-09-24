using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResult>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RefreshTokenCommandHandler(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<RefreshTokenResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var invalid = Result.Failure<RefreshTokenResult>(
            Error.Unauthorized("Users.InvalidRefreshToken", "The refresh token is invalid or has expired."));

        var tokenHash = _tokenGenerator.HashRefreshToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (user is null) return invalid;

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (existingToken is null || !existingToken.IsActive) return invalid;

        if (!user.IsActive)
            return Result.Failure<RefreshTokenResult>(Error.Unauthorized("Users.Deactivated", "This account has been deactivated."));

        // Rotation: the presented token is revoked and replaced by a brand new
        // one. This limits the blast radius of a stolen refresh token to a
        // single use and lets us detect reuse of a revoked token as a signal
        // of compromise (not wired to an alert here, but the data is captured
        // via ReplacedByTokenId for whoever builds that monitoring).
        var newAccessToken = _tokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenPlain = _tokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenGenerator.HashRefreshToken(newRefreshTokenPlain);

        var newRefreshToken = user.IssueRefreshToken(newRefreshTokenHash, RefreshTokenLifetime, request.IpAddress);
        user.RevokeRefreshToken(existingToken, newRefreshToken.Id);

        return Result.Success(new RefreshTokenResult(
            newAccessToken.Value,
            newAccessToken.ExpiresOnUtc,
            newRefreshTokenPlain,
            newRefreshToken.ExpiresOnUtc));
    }
}
