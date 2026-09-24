using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserResponse>>;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsEmailConfirmed,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedOnUtc,
    DateTime? LastLoginOnUtc);
