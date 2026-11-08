using System.Net;
using Grpc.Core;
using MARS.Platform.Infrastructure.Observability.Logging.Telegram;
using MARS.Shared.Contracts.TelegramLog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Отправитель журнала в Telegram работает по настоящему gRPC: контракт, имя
/// метода и коды ошибок проверяются настоящим клиентом и настоящим сервером.
/// </summary>
/// <remarks>
/// Через <c>TestServer</c> это не проверяется: подмена Kestrel целиком не
/// проверяет транспорт, а моками сгенерированного клиента опечатку в proto не
/// поймать — ошибка вылезла бы только на стенде.
/// </remarks>
public sealed class GrpcTelegramLogPublisherTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private RecordingLogService _service = null!;
    private Uri _address = null!;

    public async ValueTask InitializeAsync()
    {
        _service = new RecordingLogService();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        // Порт не задаётся: на машине CI он мог бы быть занят, и тест падал бы
        // не из-за своего кода. Реальный порт вычитывается у сервера.
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Kestrel does not support dynamic ports through ListenLocalhost.
            // Binding explicitly to IPv4 loopback still gives us an ephemeral port.
            options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_service);

        _app = builder.Build();
        _app.MapGrpcService<RecordingLogService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        var addresses = _app
            .Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        _address = new Uri(addresses!.Addresses.First(), UriKind.Absolute);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Главный случай: запись доходит до получателя со всеми полями. Потерянное
    /// поле означало бы сообщение, по которому нельзя понять, что сломалось.
    /// </summary>
    [Fact]
    public async Task Запись_Доходит_До_Получателя_Со_Всеми_Полями()
    {
        using var publisher = new GrpcTelegramLogPublisher(_address);
        var entry = new TelegramLogEntry
        {
            Service = "MARS.Shared.Tests",
            Level = "Error",
            Category = "MARS.Shared.Tests.Logging",
            Message = "Что-то сломалось",
            Exception = "System.InvalidOperationException: причина",
            Timestamp = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero),
            SuppressedCount = 3,
        };

        var result = await publisher.PublishAsync(entry, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Recipients);
        Assert.Equal(1, result.Suppressed);

        var request = Assert.Single(_service.Requests);
        Assert.Equal("MARS.Shared.Tests", request.Service);
        Assert.Equal("Error", request.Level);
        Assert.Equal("MARS.Shared.Tests.Logging", request.Category);
        Assert.Equal("Что-то сломалось", request.Message);
        Assert.Equal("System.InvalidOperationException: причина", request.Exception);
        Assert.Equal(3, request.SuppressedCount);
        Assert.Equal(
            entry.Timestamp.ToUnixTimeSeconds(),
            request.Timestamp.ToDateTimeOffset().ToUnixTimeSeconds()
        );
    }

    /// <summary>
    /// <c>FAILED_PRECONDITION</c> означает, что Telegram не настроен принимать
    /// журнал: повторять доставку бессмысленно, и отправитель обязан отключить
    /// её сам. Без отдельного исключения отправитель считал бы это обычным
    /// недоступением сервиса и ретраил бы до бесконечности.
    /// </summary>
    [Fact]
    public async Task Отказ_Настройки_Превращается_В_Отдельное_Исключение()
    {
        _service.FailWithPrecondition = true;
        using var publisher = new GrpcTelegramLogPublisher(_address);
        var entry = new TelegramLogEntry
        {
            Service = "MARS.Shared.Tests",
            Level = "Error",
            Message = "Что-то сломалось",
        };

        var error = await Assert.ThrowsAsync<TelegramLogNotConfiguredException>(() =>
            publisher.PublishAsync(entry, TestContext.Current.CancellationToken)
        );

        Assert.Contains("не настроен", error.Message);
        Assert.Empty(_service.Requests);
    }

    /// <summary>
    /// Получатель на время теста: записывает запросы и умеет отказать так, как
    /// отказал бы настоящий Telegram.
    /// </summary>
    public sealed class RecordingLogService : TelegramLogService.TelegramLogServiceBase
    {
        public List<LogEntryRequest> Requests { get; } = [];

        public bool FailWithPrecondition { get; set; }

        public override Task<LogEntryReply> Publish(
            LogEntryRequest request,
            ServerCallContext context
        )
        {
            if (FailWithPrecondition)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "Telegram не настроен получать журнал"
                    )
                );
            }

            Requests.Add(request);

            return Task.FromResult(new LogEntryReply { Recipients = 2, Suppressed = 1 });
        }
    }
}
