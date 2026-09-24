using FluentValidation;
using MediatR;

namespace IdentityService.Application.Common.Behaviors;

// Runs every FluentValidation validator registered for TRequest before the
// handler executes. Validation failures are translated to a Result.Failure
// (via reflection in the handler-less path is avoided — instead each command
// handler that returns Result<T> relies on this behavior short-circuiting
// with a thrown ValidationException, which the API's exception middleware
// maps to 400 Bad Request with field-level errors).
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(request, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
