using Identity.Grpc.V1;
using IdentityService.Application.Users.Queries.GetUserById;
using MediatR;
using Grpc.Core;

namespace IdentityService.Api.Services;

// Thin adapter: the gRPC endpoint delegates to the exact same MediatR query
// the REST GET /api/users/{id} endpoint uses, so there's one source of truth
// for "how do we look up a user" regardless of which transport asked.
public sealed class UserGrpcService : IdentityGrpc.IdentityGrpcBase
{
    private readonly ISender _sender;

    public UserGrpcService(ISender sender) => _sender = sender;

    public override async Task<UserReply> GetUser(GetUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id must be a valid GUID."));

        var result = await _sender.Send(new GetUserByIdQuery(userId), context.CancellationToken);

        if (result.IsFailure)
            return new UserReply { Found = false };

        var user = result.Value;
        var reply = new UserReply
        {
            Found = true,
            Id = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsEmailConfirmed = user.IsEmailConfirmed
        };
        reply.Roles.AddRange(user.Roles);
        return reply;
    }

    public override async Task<GetUsersReply> GetUsers(GetUsersRequest request, ServerCallContext context)
    {
        var reply = new GetUsersReply();

        // Fanned out in parallel rather than sequentially awaited — this is
        // the whole point of the batch RPC: one round trip, many lookups
        // resolved concurrently instead of N sequential round trips.
        var tasks = request.UserIds
            .Where(id => Guid.TryParse(id, out _))
            .Select(id => _sender.Send(new GetUserByIdQuery(Guid.Parse(id)), context.CancellationToken));

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result.IsFailure) continue;
            var user = result.Value;
            var userReply = new UserReply
            {
                Found = true,
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailConfirmed = user.IsEmailConfirmed
            };
            userReply.Roles.AddRange(user.Roles);
            reply.Users.Add(userReply);
        }

        return reply;
    }
}
