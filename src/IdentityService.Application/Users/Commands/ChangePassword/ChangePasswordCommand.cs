using IdentityService.Application.Common.Behaviors;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : IRequest<Result>, ITransactionalRequest;
