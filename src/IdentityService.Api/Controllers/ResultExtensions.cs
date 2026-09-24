using IdentityService.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

// Central mapping from the Application layer's Result/Error to HTTP status
// codes, so every controller action stays a one-liner instead of repeating
// a switch statement.
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess) return new OkResult();
        return ProblemFor(result.Error);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess) return new OkObjectResult(result.Value);
        return ProblemFor(result.Error);
    }

    private static IActionResult ProblemFor(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = statusCode
        })
        { StatusCode = statusCode };
    }
}
