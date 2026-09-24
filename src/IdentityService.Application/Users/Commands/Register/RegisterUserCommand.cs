using IdentityService.Application.Common.Behaviors;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Commands.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Result<Guid>>, ITransactionalRequest;
