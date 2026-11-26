using System;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace MARS.Gateway;

/// <summary>
/// Добавляет межсервисный ключ только к внутренним административным маршрутам.
/// Ключ не отправляется в браузер: он подставляется на Gateway при проксировании.
/// </summary>
public sealed class ServiceApiKeyHeaderTransformProvider(IOptions<ServiceAuthOptions> authOptions)
    : ITransformProvider
{
    public const string MetadataKey = "MARS.ServiceApiKey";

    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        if (
            context.Route.Metadata is null
            || !context.Route.Metadata.TryGetValue(MetadataKey, out var enabled)
            || !string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)
        )
        {
            return;
        }

        var options = authOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "ServiceAuth:ApiKey не задан для внутренней панели."
            );
        }

        context.AddRequestTransform(transformContext =>
        {
            transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                options.ApiKeyHeaderName,
                options.ApiKey
            );
            return ValueTask.CompletedTask;
        });
    }
}
