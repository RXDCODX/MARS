using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Telegram;
using MARS.Twitch.Api.Configuration;
using MARS.Twitch.Api.Services.Management;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Просьба пройти авторизацию, отправленная в <c>MARS.Telegram</c>.
/// </summary>
/// <remarks>
/// Проверяется ровно то, что дойдёт до администратора: в сообщении есть ссылка с
/// идентификатором приложения и адресом обработчика. Отказ Telegram не должен
/// ронять фоновую службу старта — вместо этого он попадает в лог.
/// </remarks>
public class TelegramAuthNotifierTests
{
    /// <summary>
    /// В сообщении есть ссылка с тем же <c>client_id</c> и тем же адресом
    /// обработчика, что уйдёт в обмене кода: Twitch сравнивает <c>redirect_uri</c>
    /// посимвольно и отвергает обмен при расхождении.
    /// </summary>
    [Fact]
    public async Task Ссылка_содержит_клиент_и_адрес_обработчика()
    {
        var telegram = new Mock<ITelegramAdminClient>();
        string? receivedService = null;
        string? receivedMessage = null;

        telegram
            .Setup(client =>
                client.SendToAdminsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (string service, string message, CancellationToken _) =>
                {
                    receivedService = service;
                    receivedMessage = message;

                    return new TelegramAdminMessageResult { Recipients = 1 };
                }
            );

        var delivered = await Create(telegram).NotifyMissingTokenAsync();

        Assert.True(delivered);
        Assert.Equal("MARS.TwitchCore", receivedService);
        Assert.NotNull(receivedMessage);
        Assert.Contains("client_id=client-из-конфигурации", receivedMessage);
        Assert.Contains(
            "redirect_uri=http://localhost:6002/api/TwitchAuth/user-auth",
            receivedMessage
        );
    }

    /// <summary>
    /// Адрес обработчика неизвестен — отправлять нечего: сообщение без пути, куда
    /// нажать, только занимает администратора.
    /// </summary>
    [Fact]
    public async Task Без_адреса_обработчика_просьба_не_уходит()
    {
        var telegram = new Mock<ITelegramAdminClient>();

        await Create(telegram, serverAddress: null).NotifyMissingTokenAsync();

        telegram.Verify(
            client =>
                client.SendToAdminsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// MARS.Telegram ответил отказом: сервис не должен на этом упасть. Отказ — это
    /// «бот не настроен», а не поломка фоновой службы.
    /// </summary>
    [Fact]
    public async Task Отказ_Telegram_не_роняет_вызывающего_и_возвращает_неудачу()
    {
        var telegram = new Mock<ITelegramAdminClient>();

        telegram
            .Setup(client =>
                client.SendToAdminsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((TelegramAdminMessageResult?)null);

        var delivered = true;
        var error = await Record.ExceptionAsync(async () =>
        {
            delivered = await Create(telegram).NotifyMissingTokenAsync();
        });

        Assert.Null(error);
        Assert.False(delivered);
    }

    /// <summary>
    /// Сосед недоступен — тоже не повод ронять старт сервиса.
    /// </summary>
    [Fact]
    public async Task Недоступность_Сервиса_не_роняет_вызывающего()
    {
        var telegram = new Mock<ITelegramAdminClient>();

        telegram
            .Setup(client =>
                client.SendToAdminsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new HttpRequestException("сервис не поднят"));

        var error = await Record.ExceptionAsync(() => Create(telegram).NotifyMissingTokenAsync());

        Assert.Null(error);
    }

    private static TelegramAuthNotifier Create(
        Mock<ITelegramAdminClient> telegram,
        string? serverAddress = "http://localhost:6002"
    ) =>
        new(
            telegram.Object,
            new TwitchAuthRedirectUriProvider(
                Options.Create(new TwitchConfiguration()),
                new FakeServer(serverAddress)
            ),
            new TwitchAPI { Settings = { ClientId = "client-из-конфигурации" } },
            NullLogger<TelegramAuthNotifier>.Instance
        );

    /// <summary>
    /// Адрес сервиса, как его видит Kestrel: без него провайдер адреса обработчика
    /// не смог бы составить ссылку.
    /// </summary>
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
