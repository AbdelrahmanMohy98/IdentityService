using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using IdentityService.Domain.Users;
using MediatR;

namespace IdentityService.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(Error.NotFound("Users.NotFound", "User not found."));

        return Result.Success(new UserResponse(
            user.Id.Value, user.Email.Value, user.FirstName, user.LastName,
            user.IsEmailConfirmed, user.Roles, user.CreatedOnUtc, user.LastLoginOnUtc));
    }
}
