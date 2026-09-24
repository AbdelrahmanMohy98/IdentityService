using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RevokeTokenCommandHandler(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenGenerator.HashRefreshToken(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);

        var invalid = Result.Failure(Error.Unauthorized("Users.InvalidRefreshToken", "The refresh token is invalid."));
        if (user is null) return invalid;

        var token = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token is null || !token.IsActive) return invalid;

        user.RevokeRefreshToken(token);
        return Result.Success();
    }
}
