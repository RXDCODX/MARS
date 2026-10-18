using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.Telegram.Configuration;
using MARS.Telegram.Grpc;
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
/// Приём просьб о сообщении администраторам от соседних сервисов.
/// </summary>
/// <remarks>
/// Сервис — единственный владелец бота, поэтому проверяется ровно то, что видит
/// вызывающий: доставлено столько, сколько вернул отправитель, и при каких данных
/// запрос отклоняется.
///
/// Отправитель здесь — заглушка: настоящий ходит в Telegram по MTProto, а его
/// методы не подменяются (в WTelegram.Client они не виртуальные), и юнит-тест,
/// проверяющий сеть, проверял бы сеть.
/// </remarks>
public class TelegramAdminGrpcServiceTests
{
    private readonly Mock<ITelegramMessageSender> _sender = new(MockBehavior.Loose);

    /// <summary>
    /// Главный случай: сообщение уходит всем адресатам из конфигурации. Пропуск
    /// одного означал бы «тихую» потерю просьбы, и владелец её бы не увидел.
    /// </summary>
    [Fact]
    public async Task Сообщение_Уходит_Всем_Администраторам()
    {
        Delivered(2);

        var reply = await CaptureAsync(
            [402763435, 42],
            client => client.SendToAdminsAsync(Request("ссылка на авторизацию"))
        );

        Assert.Equal(2, reply.Recipients);
        _sender.Verify(
            sender =>
                sender.SendToAdminsAsync("ссылка на авторизацию", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /// <summary>
    /// Без токена бота клиента в контейнере нет. Вызывающий должен получить внятный
    /// статус, а не падение на «Unable to resolve service» и не тишину.
    /// </summary>
    [Fact]
    public async Task Без_Токена_Бота_Просьба_Отклоняется()
    {
        var status = await StatusAsync(
            [42],
            client => client.SendToAdminsAsync(Request("что угодно")),
            withBot: false
        );

        Assert.Equal(StatusCode.FailedPrecondition, status.StatusCode);
        Assert.Contains("Telegram__BotToken", status.Detail);
    }

    /// <summary>
    /// Пустой список администраторов — это «слать некому», а не «доставлено ноль».
    /// Ноль молча выглядел бы как успех, и просьба потерялась бы навсегда.
    /// </summary>
    [Fact]
    public async Task Пустой_Список_Админов_Просьба_Отклоняется()
    {
        var status = await StatusAsync(
            [],
            client => client.SendToAdminsAsync(Request("что угодно"))
        );

        Assert.Equal(StatusCode.FailedPrecondition, status.StatusCode);
        Assert.Contains("Telegram__AdminIds__0", status.Detail);
    }

    /// <summary>
    /// Пустое сообщение — ошибка вызывающего, а не состояние Telegram: статусы
    /// разные, и действия по ним тоже разные.
    /// </summary>
    [Fact]
    public async Task Пустое_Сообщение_Отклоняется()
    {
        var status = await StatusAsync([42], client => client.SendToAdminsAsync(Request("   ")));

        Assert.Equal(StatusCode.InvalidArgument, status.StatusCode);
    }

    /// <summary>
    /// А вот когда не доставил никому — это уже ошибка доставки, и вызывающий
    /// должен узнать о ней статусом, иначе просьба считалась бы выполненной.
    /// </summary>
    [Fact]
    public async Task Когда_Не_Доставил_Никому_Просьба_Падает()
    {
        Delivered(0);

        var status = await StatusAsync(
            [1, 2],
            client => client.SendToAdminsAsync(Request("ссылка"))
        );

        Assert.Equal(StatusCode.Unavailable, status.StatusCode);
    }

    private AdminMessageRequest Request(string message) =>
        new() { Service = "MARS.TwitchCore", Message = message };

    /// <summary>
    /// Отправитель сообщает, сколько администраторов получили сообщение.
    /// </summary>
    private void Delivered(int count) =>
        _sender
            .Setup(sender =>
                sender.SendToAdminsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(count);

    /// <summary>
    /// Вызов, который обязан вернуться ответом.
    /// </summary>
    private async Task<TResponse> CaptureAsync<TResponse>(
        long[] admins,
        Func<TelegramAdminService.TelegramAdminServiceClient, AsyncUnaryCall<TResponse>> call,
        bool withBot = true
    ) => await WithClientAsync(admins, call, withBot);

    /// <summary>
    /// Вызов, который обязан упасть статусом: возвращается он сам, чтобы тест
    /// утверждал именно статус, а не «что-то исключение».
    /// </summary>
    private async Task<Status> StatusAsync(
        long[] admins,
        Func<
            TelegramAdminService.TelegramAdminServiceClient,
            AsyncUnaryCall<AdminMessageReply>
        > call,
        bool withBot = true
    )
    {
        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await WithClientAsync(admins, call, withBot)
        );

        return error.Status;
    }

    private async Task<TResponse> WithClientAsync<TResponse>(
        long[] admins,
        Func<TelegramAdminService.TelegramAdminServiceClient, AsyncUnaryCall<TResponse>> call,
        bool withBot
    )
    {
        _sender.Invocations.Clear();

        var options = Options.Create(
            new TelegramConfiguration
            {
                AdminIds = admins,
                // Токен обязателен: без него запрос отклоняется раньше отправки,
                // и проверка списка администраторов не была бы достигнута.
                BotToken = withBot ? "123456:заглушка" : string.Empty,
            }
        );

        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(
            new TelegramAdminGrpcService(
                _sender.Object,
                options,
                NullLogger<TelegramAdminGrpcService>.Instance
            )
        );

        await using var app = builder.Build();
        app.MapGrpcService<TelegramAdminGrpcService>();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var channel = GrpcChannel.ForAddress(
            "http://telegram.test",
            new GrpcChannelOptions { HttpHandler = app.GetTestServer().CreateHandler() }
        );

        return await call(
            new TelegramAdminService.TelegramAdminServiceClient(channel)
        ).ResponseAsync;
    }
}
