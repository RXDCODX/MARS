using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Alerts.Control;
using MARS.Shared.Contracts.Commands.Control;
using MARS.Shared.Contracts.Discord.Control;
using MARS.Shared.Contracts.MediaStorage;
using MARS.Shared.Contracts.Shikimori;
using MARS.Shared.Contracts.SoundRequest.Control;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Shared.Contracts.TelegramLog;
using MARS.Shared.Contracts.TelegramMessage;
using MARS.Shared.Contracts.Tts.Control;
using MARS.Shared.Contracts.Twitch.Control;
using MARS.Shared.Contracts.WaifuGacha.Control;
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

    [Theory]
    [InlineData("MARS.Twitch")]
    [InlineData("Twitch")]
    [InlineData("MARS.TwitchCore")]
    [InlineData("TwitchCore")]
    public void TwitchAliasesCannotCallTwitchFacade(string serviceName)
    {
        var identity = new ServiceIdentity(serviceName);
        var client = new MarsGrpcClient(null!, identity);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = client.Twitch);

        Assert.Contains("self-call", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(AlertsControlService.AlertsControlServiceClient))]
    [InlineData(typeof(CommandsControlService.CommandsControlServiceClient))]
    [InlineData(typeof(WaifuGachaService.WaifuGachaServiceClient))]
    [InlineData(typeof(TwitchControlService.TwitchControlServiceClient))]
    [InlineData(typeof(ShikimoriService.ShikimoriServiceClient))]
    [InlineData(typeof(MediaStorageService.MediaStorageServiceClient))]
    [InlineData(typeof(DiscordMessageService.DiscordMessageServiceClient))]
    [InlineData(typeof(TelegramAdminService.TelegramAdminServiceClient))]
    [InlineData(typeof(TelegramLogService.TelegramLogServiceClient))]
    [InlineData(typeof(TelegramMessageService.TelegramMessageServiceClient))]
    [InlineData(typeof(SoundRequestControlService.SoundRequestControlServiceClient))]
    [InlineData(typeof(TtsControlService.TtsControlServiceClient))]
    public void EveryGrpcFacadeHasRegisteredGeneratedClient(Type clientType)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceIdentity>(new ServiceIdentity("MARS.Shared.Tests"));
        services.AddMarsGrpcClient();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService(clientType));
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
            client.Telegram.Logs.PublishAsync(
                new LogEntryRequest(),
                TestContext.Current.CancellationToken
            )
        );
    }
}
