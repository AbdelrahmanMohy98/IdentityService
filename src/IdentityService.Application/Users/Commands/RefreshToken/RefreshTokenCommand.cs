using IdentityService.Application.Common.Behaviors;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress)
    : IRequest<Result<RefreshTokenResult>>, ITransactionalRequest;

public sealed record RefreshTokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresOnUtc);
