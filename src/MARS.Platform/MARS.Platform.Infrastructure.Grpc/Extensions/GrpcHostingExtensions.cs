using Grpc.AspNetCore.Server;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Platform.Infrastructure.Grpc.Extensions;

public static class GrpcHostingExtensions
{
    public static WebApplicationBuilder AddMarsGrpcHosting(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(GrpcOptions.SectionName).Get<GrpcOptions>()
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