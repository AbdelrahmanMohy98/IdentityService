using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using IdentityService.Domain.Users;
using MediatR;

namespace IdentityService.Application.Users.Commands.Register;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            return Result.Failure<Guid>(Error.Conflict("Users.EmailAlreadyExists", "An account with this email already exists."));

        var email = Email.Create(request.Email);
        var hashedPassword = HashedPassword.FromHash(_passwordHasher.Hash(request.Password));

        var user = User.Register(email, hashedPassword, request.FirstName, request.LastName);
        _userRepository.Add(user);

        // Note: no explicit SaveChanges call here — UnitOfWorkBehavior commits
        // after this handler returns, since the command implements ITransactionalRequest.
        return Result.Success(user.Id.Value);
    }
}
