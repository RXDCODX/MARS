using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.TelegramLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Grpc;

public class MarsGrpcClientTests
{
    [Fact]
    public void SelfService_CannotAccessOwnGrpcProperty()
    {
        var identity = new ServiceIdentity("MARS.Telegram");
        var client = new MarsGrpcClient(null!, identity);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = client.Telegram);

        Assert.Contains("self-call", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DifferentService_CanAccessTelegramFacade()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceIdentity>(new ServiceIdentity("MARS.Alerts"));
        services.Configure<GrpcOptions>(options =>
        {
            options.Endpoints["Telegram"] = "http://telegram:8081";
        });
        services.AddMarsGrpcClient();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMarsGrpcClient>();

        var first = client.Telegram;
        var second = client.Telegram;

        Assert.Same(first, second);
        Assert.IsAssignableFrom<ITelegramApi>(first);
    }

    [Fact]
    public async Task MissingEndpoint_ThrowsWhenFacadeIsUsed()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceIdentity>(new ServiceIdentity("MARS.Alerts"));
        services.Configure<GrpcOptions>(options => options.Endpoints.Clear());
        services.AddMarsGrpcClient();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMarsGrpcClient>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.Telegram.Logs.PublishAsync(new LogEntryRequest(), TestContext.Current.CancellationToken)
        );
    }
}