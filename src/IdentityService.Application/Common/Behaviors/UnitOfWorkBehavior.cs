using IdentityService.Application.Common.Interfaces;
using IdentityService.Application.Common.Models;
using MediatR;

namespace IdentityService.Application.Common.Behaviors;

// Only commands (not queries) implement ITransactionalRequest, so SaveChanges
// is called exactly once per write use case, after the handler has mutated
// the aggregate in memory — keeping "when do we hit the database" out of
// every individual handler.
public interface ITransactionalRequest { }

public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ITransactionalRequest && response is Result { IsSuccess: true })
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
