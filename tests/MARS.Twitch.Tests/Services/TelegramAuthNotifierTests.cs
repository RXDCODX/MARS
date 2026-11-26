using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Twitch.Application.Configuration;
using MARS.Twitch.Infrastructure.Services.Management;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api;

namespace MARS.Twitch.Tests.Services;

public class TelegramAuthNotifierTests
{
    [Fact]
    public async Task Ссылка_содержит_клиент_и_адрес_обработчика()
    {
        var (grpc, admin) = CreateMocks();
        string? receivedService = null;
        string? receivedMessage = null;

        admin
            .Setup(client =>
                client.SendToAdminsAsync(
                    It.IsAny<AdminMessageRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (AdminMessageRequest request, CancellationToken _) =>
                {
                    receivedService = request.Service;
                    receivedMessage = request.Message;
                    return new AdminMessageReply { Recipients = 1 };
                }
            );

        var delivered = await Create(grpc).NotifyMissingTokenAsync();

        Assert.True(delivered);
        Assert.Equal("MARS.TwitchCore", receivedService);
        Assert.NotNull(receivedMessage);
        Assert.Contains("client_id=client-из-конфигурации", receivedMessage);
        Assert.Contains(
            "redirect_uri=http://localhost:6002/api/TwitchAuth/user-auth",
            receivedMessage
        );
    }

    [Fact]
    public async Task Без_адреса_обработчика_просьба_не_уходит()
    {
        var (grpc, admin) = CreateMocks();

        await Create(grpc, serverAddress: null).NotifyMissingTokenAsync();

        admin.Verify(
            client =>
                client.SendToAdminsAsync(
                    It.IsAny<AdminMessageRequest>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Отказ_Telegram_не_роняет_вызывающего_и_возвращает_неудачу()
    {
        var (grpc, admin) = CreateMocks();

        admin
            .Setup(client =>
                client.SendToAdminsAsync(
                    It.IsAny<AdminMessageRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(
                new RpcException(new Status(StatusCode.FailedPrecondition, "Telegram не настроен"))
            );

        var delivered = true;
        var error = await Record.ExceptionAsync(async () =>
        {
            delivered = await Create(grpc).NotifyMissingTokenAsync();
        });

        Assert.Null(error);
        Assert.False(delivered);
    }

    [Fact]
    public async Task Недоступность_Сервиса_не_роняет_вызывающего()
    {
        var (grpc, admin) = CreateMocks();

        admin
            .Setup(client =>
                client.SendToAdminsAsync(
                    It.IsAny<AdminMessageRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unavailable, "сервис не поднят")));

        var error = await Record.ExceptionAsync(() => Create(grpc).NotifyMissingTokenAsync());

        Assert.Null(error);
    }

    private static (Mock<IMarsGrpcClient> Grpc, Mock<ITelegramAdminApi> Admin) CreateMocks()
    {
        var admin = new Mock<ITelegramAdminApi>();
        var telegramApi = new Mock<ITelegramApi>();
        telegramApi.SetupGet(api => api.Admin).Returns(admin.Object);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.Telegram).Returns(telegramApi.Object);
        return (grpc, admin);
    }

    private static TelegramAuthNotifier Create(
        Mock<IMarsGrpcClient> grpc,
        string? serverAddress = "http://localhost:6002"
    ) =>
        new(
            grpc.Object,
            new TwitchAuthRedirectUriProvider(
                Options.Create(new TwitchConfiguration()),
                new FakeServer(serverAddress)
            ),
            new TwitchAPI { Settings = { ClientId = "client-из-конфигурации" } },
            NullLogger<TelegramAuthNotifier>.Instance
        );

    private sealed class FakeServer(string? address) : IServer
    {
        public IFeatureCollection Features { get; } = Build(address);

        private static FeatureCollection Build(string? address)
        {
            var features = new FeatureCollection();
            var addresses = new ServerAddressesFeature();

            if (address is not null)
            {
                addresses.Addresses.Add(address);
            }

            features.Set<IServerAddressesFeature>(addresses);

            return features;
        }

        public Task StartAsync<TContext>(
            IHttpApplication<TContext> application,
            CancellationToken cancellationToken = default
        )
            where TContext : notnull => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Dispose() { }
    }
}
