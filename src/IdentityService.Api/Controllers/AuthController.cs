using IdentityService.Api.Contracts;
using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Users.Commands.ChangePassword;
using IdentityService.Application.Users.Commands.Login;
using IdentityService.Application.Users.Commands.RefreshToken;
using IdentityService.Application.Users.Commands.Register;
using IdentityService.Application.Users.Commands.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public AuthController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterUserCommand(request.Email, request.Password, request.FirstName, request.LastName),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Email, request.Password, _currentUser.IpAddress),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(request.RefreshToken, _currentUser.IpAddress),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("revoke")]
    [AllowAnonymous] // revocation only requires possessing the refresh token itself
    public async Task<IActionResult> Revoke(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RevokeTokenCommand(request.RefreshToken), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value; // guaranteed by [Authorize]
        var result = await _sender.Send(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return result.ToActionResult();
    }
}
