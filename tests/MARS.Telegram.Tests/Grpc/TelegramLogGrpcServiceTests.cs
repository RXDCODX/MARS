using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc.TelegramLog;
using MARS.Telegram.Configuration;
using MARS.Telegram.Grpc;
using MARS.Telegram.Services.Logging;
using MARS.Telegram.Services.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Telegram.Tests.Grpc;

/// <summary>
/// Приём записей журнала от соседних сервисов.
///
/// Сервис — единственный владелец бота, поэтому проверяется ровно то, что видит
/// вызывающий: кому ушло, сколько чатов получило и при каких данных запрос
/// отклоняется.
///
/// Отправитель здесь — заглушка: настоящий ходит в Telegram по MTProto, а его
/// методы не подменяются (в WTelegram.Client они не виртуальные), и юнит-тест,
/// проверяющий сеть, проверял бы сеть.
/// </summary>
public class TelegramLogGrpcServiceTests
{
    private readonly Mock<ITelegramMessageSender> _sender = new(MockBehavior.Loose);

    /// <summary>
    /// Главный случай: запись уходит в чаты из <c>Telegram:LogChatIds</c>. Отдельный
    /// чат для журнала — чтобы его нельзя было спутать с чатом администраторов.
    /// </summary>
    [Fact]
    public async Task Запись_Уходит_В_Чаты_Для_Журнала()
    {
        Delivered(2);

        var reply = await CaptureAsync(
            config: new TelegramConfiguration
            {
                BotToken = "123456:заглушка",
                LogChatIds = [100, 200],
                AdminIds = [42],
            },
            call: client => client.PublishAsync(Request())
        );

        Assert.Equal(2, reply.Recipients);
        _sender.Verify(
            sender =>
                sender.SendPlainToChatsAsync(
                    It.Is<IReadOnlyList<long>>(chats =>
                        chats.SequenceEqual(new long[] { 100, 200 })
                    ),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Отдельного чата для журнала может не быть — тогда журнал уходит
    /// администраторам. Откат на администраторов лучше, чем отсутствие
    /// журнала вовсе.
    /// </summary>
    [Fact]
    public async Task Без_Чатов_Для_Журнала_Берутся_Администраторы()
    {
        Delivered(1);

        var reply = await CaptureAsync(
            config: new TelegramConfiguration
            {
                BotToken = "123456:заглушка",
                LogChatIds = [],
                AdminIds = [42],
            },
            call: client => client.PublishAsync(Request())
        );

        Assert.Equal(1, reply.Recipients);
        _sender.Verify(
            sender =>
                sender.SendPlainToChatsAsync(
                    It.Is<IReadOnlyList<long>>(chats => chats.SequenceEqual(new long[] { 42 })),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Без токена бота клиента в контейнере нет. Вызывающий должен получить
    /// внятный статус, а не падение на «Unable to resolve service».
    /// </summary>
    [Fact]
    public async Task Без_Токена_Бота_Запись_Отклоняется()
    {
        var status = await StatusAsync(
            config: new TelegramConfiguration { LogChatIds = [100] },
            call: client => client.PublishAsync(Request())
        );

        Assert.Equal(StatusCode.FailedPrecondition, status.StatusCode);
        Assert.Contains("Telegram__BotToken", status.Detail);
    }

    /// <summary>
    /// Пустая запись — ошибка вызывающего, а не состояние Telegram: статусы
    /// разные, и действия по ним тоже разные.
    /// </summary>
    [Fact]
    public async Task Пустая_Запись_Отклоняется()
    {
        var status = await StatusAsync(
            config: new TelegramConfiguration { BotToken = "123456:заглушка", LogChatIds = [100] },
            call: client => client.PublishAsync(Request(message: "   "))
        );

        Assert.Equal(StatusCode.InvalidArgument, status.StatusCode);
    }

    /// <summary>
    /// Ни чата для журнала, ни администратора — слать некому. Это состояние
    /// службы, а не ошибка доставки.
    /// </summary>
    [Fact]
    public async Task Пустой_Список_Чатов_Отклоняется()
    {
        var status = await StatusAsync(
            config: new TelegramConfiguration { BotToken = "123456:заглушка" },
            call: client => client.PublishAsync(Request())
        );

        Assert.Equal(StatusCode.FailedPrecondition, status.StatusCode);
        Assert.Contains("Telegram__LogChatIds__0", status.Detail);
    }

    /// <summary>
    /// А вот когда не доставил никому — это уже ошибка доставки, и вызывающий
    /// должен узнать о ней статусом, иначе запись считалась бы доставленной.
    /// </summary>
    [Fact]
    public async Task Когда_Не_Доставил_Никому_Запись_Падает()
    {
        Delivered(0);

        var status = await StatusAsync(
            config: new TelegramConfiguration
            {
                BotToken = "123456:заглушка",
                LogChatIds = [100, 200],
            },
            call: client => client.PublishAsync(Request())
        );

        Assert.Equal(StatusCode.Unavailable, status.StatusCode);
    }

    /// <summary>
    /// Записи сверх лимита отбрасываются, и вызывающий узнаёт об этом числом:
    /// молчаливая потеря выглядела бы как «доставилось всё».
    /// </summary>
    /// <remarks>
    /// Лимитер общий на оба вызова: он и должен считать записи сервиса за
    /// минуту, а не за вызов.
    /// </remarks>
    [Fact]
    public async Task Записи_Сверх_Лимита_Отбрасываются()
    {
        Delivered(1);
        var config = new TelegramConfiguration { BotToken = "123456:заглушка", LogChatIds = [100] };
        var limiter = new TelegramLogRateLimiter(1);

        var first = await CaptureAsync(
            config: config,
            call: client => client.PublishAsync(Request()),
            limiter: limiter
        );
        var second = await CaptureAsync(
            config: config,
            call: client => client.PublishAsync(Request()),
            limiter: limiter
        );

        Assert.Equal(1, first.Recipients);
        Assert.Equal(0, second.Recipients);
        Assert.Equal(1, second.Suppressed);
    }

    private static LogEntryRequest Request(string message = "Что-то сломалось") =>
        new()
        {
            Service = "MARS.TwitchCore",
            Level = "Error",
            Category = "MARS.TwitchCore.Services.Rewards",
            Message = message,
        };

    /// <summary>
    /// Отправитель сообщает, сколько чатов получили запись.
    /// </summary>
    private void Delivered(int count) =>
        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(count);

    private async Task<LogEntryReply> CaptureAsync(
        TelegramConfiguration config,
        Func<TelegramLogService.TelegramLogServiceClient, AsyncUnaryCall<LogEntryReply>> call,
        TelegramLogRateLimiter? limiter = null
    ) => await WithClientAsync(config, call, limiter);

    private async Task<Status> StatusAsync(
        TelegramConfiguration config,
        Func<TelegramLogService.TelegramLogServiceClient, AsyncUnaryCall<LogEntryReply>> call
    )
    {
        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await WithClientAsync(config, call, null)
        );

        return error.Status;
    }

    private async Task<LogEntryReply> WithClientAsync(
        TelegramConfiguration config,
        Func<TelegramLogService.TelegramLogServiceClient, AsyncUnaryCall<LogEntryReply>> call,
        TelegramLogRateLimiter? limiter
    )
    {
        _sender.Invocations.Clear();

        var options = Options.Create(config);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(
            new TelegramLogGrpcService(
                _sender.Object,
                options,
                limiter ?? new TelegramLogRateLimiter(1000),
                NullLogger<TelegramLogGrpcService>.Instance
            )
        );

        await using var app = builder.Build();
        app.MapGrpcService<TelegramLogGrpcService>();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var channel = GrpcChannel.ForAddress(
            "http://telegram.test",
            new GrpcChannelOptions { HttpHandler = app.GetTestServer().CreateHandler() }
        );

        return await call(new TelegramLogService.TelegramLogServiceClient(channel)).ResponseAsync;
    }
}
