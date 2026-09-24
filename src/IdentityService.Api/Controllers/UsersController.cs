using IdentityService.Application.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        // With [Authorize] enforced, a missing/unparsable "sub" claim means
        // the token itself is malformed rather than a normal 401 case, so
        // this fails loudly (500) instead of silently — which is exactly
        // what would have surfaced this bug immediately as a clear error
        // instead of a bare NullReferenceException.
        var subClaim = User.FindFirst("sub")
            ?? throw new InvalidOperationException("Authenticated request is missing the 'sub' claim.");

        var userId = Guid.Parse(subClaim.Value);
        var result = await _sender.Send(new GetUserByIdQuery(userId), cancellationToken);
        return result.ToActionResult();
    }
}
