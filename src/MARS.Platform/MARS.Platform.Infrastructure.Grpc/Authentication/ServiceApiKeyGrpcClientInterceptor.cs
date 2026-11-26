using Grpc.Core;
using Grpc.Core.Interceptors;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Grpc.Authentication;

public sealed class ServiceApiKeyGrpcClientInterceptor(
    IOptionsMonitor<ServiceAuthOptions> authOptions
) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation
    )
    {
        var settings = authOptions.CurrentValue;
        if (settings.IsEnabled)
        {
            var metadata = context.Options.Headers ?? new Metadata();
            metadata.Add(
                settings.ApiKeyHeaderName.ToLowerInvariant(),
                settings.ApiKey ?? string.Empty
            );
            var options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                options
            );
        }

        return continuation(request, context);
    }
}
