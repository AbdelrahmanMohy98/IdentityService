using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    // Refresh token lifetime is a policy decision, not a domain rule, so it
    // lives here rather than on the aggregate — easy to make configurable later.
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same generic error for "no such user" and "wrong password" —
        // never reveal which one it was, that's an account-enumeration leak.
        var invalidCredentials = Result.Failure<LoginResult>(
            Error.Unauthorized("Users.InvalidCredentials", "Invalid email or password."));

        if (user is null)
            return invalidCredentials;

        if (!user.IsActive)
            return Result.Failure<LoginResult>(Error.Unauthorized("Users.Deactivated", "This account has been deactivated."));

        if (user.IsLockedOut)
            return Result.Failure<LoginResult>(Error.Unauthorized("Users.LockedOut",
                $"Account is locked until {user.LockedOutUntilUtc:u} due to repeated failed login attempts."));

        if (!_passwordHasher.Verify(request.Password, user.Password.Value))
        {
            user.RegisterFailedLoginAttempt();
            return invalidCredentials;
        }

        user.RegisterSuccessfulLogin(request.IpAddress);

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshTokenPlain = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _tokenGenerator.HashRefreshToken(refreshTokenPlain);

        var refreshToken = user.IssueRefreshToken(refreshTokenHash, RefreshTokenLifetime, request.IpAddress);

        return Result.Success(new LoginResult(
            user.Id.Value,
            accessToken.Value,
            accessToken.ExpiresOnUtc,
            refreshTokenPlain,
            refreshToken.ExpiresOnUtc));
    }
}
