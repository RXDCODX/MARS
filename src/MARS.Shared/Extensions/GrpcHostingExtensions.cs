using Google.Protobuf;
using Grpc.AspNetCore.Server;
using MARS.Shared.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Extensions;

public static class GrpcHostingExtensions
{
    public const int HttpPort = 8080;
    public const int GrpcPort = 8081;

    /// <summary>
    /// Поднимает gRPC рядом с REST. Два отдельных порта обязательны: Kestrel
    /// обслуживает HTTP/2 без TLS только на эндпоинте, у которого протокол
    /// задан явно как Http2, а эндпоинт из ASPNETCORE_HTTP_PORTS всегда
    /// остаётся HTTP/1.1 — проверил на живом запуске, Requests.VersionPolicy
    /// с prior knowledge получал HTTP_1_1_REQUIRED и на Http1AndHttp2.
    /// </summary>
    public static WebApplicationBuilder AddMarsGrpcHosting(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(HttpPort, listen => listen.Protocols = HttpProtocols.Http1);
            options.ListenAnyIP(GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddGrpc();

        return builder;
    }

    public static IServiceCollection AddMarsEventBroadcaster<TMessage>(
        this IServiceCollection services
    )
        where TMessage : class, IMessage<TMessage>, new()
    {
        services.AddSingleton<GrpcEventBroadcaster<TMessage>>();

        return services;
    }
}
