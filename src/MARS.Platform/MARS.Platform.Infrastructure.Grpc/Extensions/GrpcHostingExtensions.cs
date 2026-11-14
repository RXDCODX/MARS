using Grpc.AspNetCore.Server;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Platform.Infrastructure.Grpc.Extensions;

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
    ///
    /// Порты берутся из <see cref="GrpcOptions"/>, а не объявляются кодом: на
    /// хосте 8080/8081 обычно заняты стендом, а <c>ListenAnyIP</c> подавляет
    /// <c>ASPNETCORE_URLS</c> и <c>applicationUrl</c> из профиля запуска, так что
    /// другого способа сдвинуть эти два порта нет.
    /// </summary>
    public static WebApplicationBuilder AddMarsGrpcHosting(this WebApplicationBuilder builder)
    {
        var options =
            builder.Configuration.GetSection(GrpcOptions.SectionName).Get<GrpcOptions>()
            ?? new GrpcOptions();

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.ListenAnyIP(options.HttpPort, listen => listen.Protocols = HttpProtocols.Http1);
            kestrel.ListenAnyIP(options.GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddGrpc();

        return builder;
    }
}
