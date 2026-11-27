using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Grpc.Authentication;

/// <summary>
/// Проверяет межсервисный ключ на gRPC-границе. REST-политика не применяется к
/// HTTP/2-вызовам автоматически, поэтому каждый gRPC-сервис получает одинаковую
/// fail-closed проверку через глобальный интерцептор.
/// </summary>
public sealed class ServiceApiKeyGrpcServerInterceptor(
    IOptionsMonitor<ServiceAuthOptions> authOptions
) : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        var settings = authOptions.CurrentValue;
        if (settings.IsEnabled && !IsAuthorized(context.RequestHeaders, settings))
        {
            throw new RpcException(
                new Status(StatusCode.Unauthenticated, "A valid service API key is required.")
            );
        }

        return continuation(request, context);
    }

    internal static bool IsAuthorized(Metadata headers, ServiceAuthOptions settings)
    {
        if (!settings.IsEnabled)
        {
            return true;
        }

        var supplied = headers
            .FirstOrDefault(header =>
                string.Equals(
                    header.Key,
                    settings.ApiKeyHeaderName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            ?.Value;

        if (string.IsNullOrEmpty(supplied) || string.IsNullOrEmpty(settings.ApiKey))
        {
            return false;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(settings.ApiKey);

        return suppliedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
