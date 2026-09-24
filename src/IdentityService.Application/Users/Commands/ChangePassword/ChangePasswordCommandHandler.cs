using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using IdentityService.Domain.Users;
using MediatR;

namespace IdentityService.Application.Users.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
        if (user is null)
            return Result.Failure(Error.NotFound("Users.NotFound", "User not found."));

        if (!_passwordHasher.Verify(request.CurrentPassword, user.Password.Value))
            return Result.Failure(Error.Validation("Users.WrongPassword", "Current password is incorrect."));

        user.ChangePassword(HashedPassword.FromHash(_passwordHasher.Hash(request.NewPassword)));
        return Result.Success();
    }
}
