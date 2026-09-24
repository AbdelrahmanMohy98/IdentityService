using IdentityService.Application.Common.Behaviors;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<Result>, ITransactionalRequest;
