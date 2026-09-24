using IdentityService.Application.Common.Behaviors;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress)
    : IRequest<Result<LoginResult>>, ITransactionalRequest;

public sealed record LoginResult(
    Guid UserId,
    string AccessToken,
    DateTime AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresOnUtc);
