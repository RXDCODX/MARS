using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Services.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api;
using TwitchAdminServiceClient = MARS.Shared.Grpc.TelegramAdmin.TelegramAdminService.TelegramAdminServiceClient;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Просьба пройти авторизацию, отправленная в <c>MARS.Telegram</c>.
/// </summary>
/// <remarks>
/// Проверяется ровно то, что дойдёт до администратора: в сообщении есть ссылка с
/// идентификатором приложения и адресом обработчика. Отказ Telegram не должен
/// ронять фоновую службу старта — вместо этого он попадает в лог.
/// </remarks>
public class GrpcTwitchAuthNotifierTests
{
    /// <summary>
    /// В сообщении есть ссылка с тем же <c>client_id</c> и тем же адресом
    /// обработчика, что уйдёт в обмене кода: Twitch сравнивает <c>redirect_uri</c>
    /// посимвольно и отвергает обмен при расхождении.
    /// </summary>
    [Fact]
    public async Task Ссылка_содержит_клиент_и_адрес_обработчика()
    {
        var received = new List<AdminMessageRequest>();

        await WithTelegramAsync(
            request =>
            {
                received.Add(request);
                return new AdminMessageReply { Recipients = 1 };
            },
            async notifier => await notifier.NotifyMissingTokenAsync()
        );

        Assert.Single(received);
        Assert.Contains("client_id=client-из-конфигурации", received[0].Message);
        Assert.Contains(
            "redirect_uri=http://localhost:6002/api/TwitchAuth/user-auth",
            received[0].Message
        );
        Assert.Equal("MARS.TwitchCore", received[0].Service);
    }

    /// <summary>
    /// Адрес обработчика неизвестен — отправлять нечего: сообщение без пути, куда
    /// нажать, только занимает администратора.
    /// </summary>
    [Fact]
    public async Task Без_адреса_обработчика_просьба_не_уходит()
    {
        var received = new List<AdminMessageRequest>();

        await WithTelegramAsync(
            request =>
            {
                received.Add(request);
                return new AdminMessageReply { Recipients = 1 };
            },
            async notifier => await notifier.NotifyMissingTokenAsync(),
            serverAddress: null
        );

        Assert.Empty(received);
    }

    /// <summary>
    /// MARS.Telegram ответил отказом: сервис не должен на этом упасть. Отказ — это
    /// «бот не настроен», а не поломка фоновой службы.
    /// </summary>
    [Fact]
    public async Task Отказ_Telegram_не_роняет_вызывающего()
    {
        var error = await Record.ExceptionAsync(async () =>
            await WithTelegramAsync(
                _ =>
                    throw new RpcException(
                        new Status(StatusCode.FailedPrecondition, "Telegram не настроен")
                    ),
                async notifier => await notifier.NotifyMissingTokenAsync()
            )
        );

        Assert.Null(error);
    }

    /// <summary>
    /// Сосед недоступен — тоже не повод ронять старт сервиса.
    /// </summary>
    [Fact]
    public async Task Недоступность_Сервиса_не_роняет_вызывающего()
    {
        var error = await Record.ExceptionAsync(async () =>
            await WithTelegramAsync(
                _ => throw new RpcException(new Status(StatusCode.Unavailable, "сервис не поднят")),
                async notifier => await notifier.NotifyMissingTokenAsync()
            )
        );

        Assert.Null(error);
    }

    private static async Task WithTelegramAsync(
        Func<AdminMessageRequest, AdminMessageReply> onMessage,
        Func<GrpcTwitchAuthNotifier, Task> action,
        string? serverAddress = "http://localhost:6002"
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(new RecordingTelegramAdminService(onMessage));

        await using var app = builder.Build();
        app.MapGrpcService<RecordingTelegramAdminService>();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var channel = GrpcChannel.ForAddress(
            "http://telegram.test",
            new GrpcChannelOptions { HttpHandler = app.GetTestServer().CreateHandler() }
        );

        var notifier = new GrpcTwitchAuthNotifier(
            new TwitchAdminServiceClient(channel),
            new TwitchAuthRedirectUriProvider(
                Options.Create(new TwitchConfiguration()),
                new FakeServer(serverAddress)
            ),
            new TwitchAPI { Settings = { ClientId = "client-из-конфигурации" } },
            NullLogger<GrpcTwitchAuthNotifier>.Instance
        );

        await action(notifier);
    }

    /// <summary>
    /// Приёмник на стороне соседа: настоящий контракт вместо заглушки на клиенте,
    /// чтобы проверялись и сериализация, и маршрутизация.
    /// </summary>
    private sealed class RecordingTelegramAdminService(
        Func<AdminMessageRequest, AdminMessageReply> onMessage
    ) : TelegramAdminService.TelegramAdminServiceBase
    {
        public override Task<AdminMessageReply> SendToAdmins(
            AdminMessageRequest request,
            ServerCallContext context
        ) => Task.FromResult(onMessage(request));
    }

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
