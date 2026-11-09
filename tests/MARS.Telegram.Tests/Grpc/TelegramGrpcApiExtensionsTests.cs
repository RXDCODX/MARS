using MARS.Telegram.Api.Grpc;
using MARS.Telegram.Api.Grpc.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Telegram.Tests.Grpc;

/// <summary>Проверяет подключение gRPC-адаптера к composition root.</summary>
public class TelegramGrpcApiExtensionsTests
{
    [Fact]
    public async Task RegistersAndMapsBothServices()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddGrpc();

        var services = builder.Services.AddTelegramGrpcApi();

        Assert.Same(builder.Services, services);
        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(TelegramAdminGrpcService)
        );
        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(TelegramLogGrpcService)
        );

        await using var app = builder.Build();

        Assert.Same(app, app.MapTelegramGrpcApi());
    }
}
