using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace IdentityService.Api.Grpc;

public sealed class GrpcSecurityOptions
{
    public const string SectionName = "Grpc";
    public string InternalApiKey { get; init; } = default!;
}

// Deliberately simple: a shared secret sent as gRPC metadata, checked on
// every call. This is *not* the end-user's JWT — it authenticates the calling
// *service* (e.g. OrdersService), distinct from the end user whose data is
// being requested. Good enough inside a private network/VPC or service mesh;
// swap for mTLS (client certs) at the Kestrel/channel level if the network
// boundary isn't trusted, without touching this interceptor's shape.
public sealed class InternalApiKeyInterceptor : Interceptor
{
    private const string MetadataKey = "x-internal-api-key";
    private readonly GrpcSecurityOptions _options;

    public InternalApiKeyInterceptor(IOptions<GrpcSecurityOptions> options) => _options = options.Value;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var providedKey = context.RequestHeaders.GetValue(MetadataKey);

        if (string.IsNullOrEmpty(_options.InternalApiKey) ||
            !string.Equals(providedKey, _options.InternalApiKey, StringComparison.Ordinal))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid internal API key."));
        }

        return await continuation(request, context);
    }
}
