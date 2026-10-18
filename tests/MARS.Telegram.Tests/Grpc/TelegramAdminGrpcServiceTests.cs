using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.Telegram.Configuration;
using MARS.Telegram.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;

namespace MARS.Telegram.Tests.Grpc;

/// <summary>
/// Приём просьб о сообщении администраторам от соседних сервисов.
/// </summary>
/// <remarks>
/// Сервис — единственный владелец бота, поэтому проверяется ровно то, что видит
/// администратор: кому ушло сообщение и что в нём написано. Заглушка бота
/// перехватывает не <c>SendMessage</c>, а <c>SendRequest</c>: в Telegram.Bot
/// <c>SendMessage</c> — расширяющий метод поверх интерфейса, и перечислять его
/// необязательные параметры ради проверки означало бы сломать тест при любом
/// обновлении пакета.
/// </remarks>
public class TelegramAdminGrpcServiceTests
{
    private readonly Mock<ITelegramBotClient> _bot = new(MockBehavior.Loose);

    /// <summary>
    /// Главный случай: сообщение уходит всем адресатам из конфигурации. Пропуск
    /// одного означал бы «тихую» потерю просьбы, и владелец её бы не увидел.
    /// </summary>
    [Fact]
    public async Task Сообщение_Уходит_Всем_Администраторам()
    {
        var reply = await CaptureAsync(
            [402763435, 42],
            client => client.SendToAdminsAsync(Request("ссылка на авторизацию"))
        );

        Assert.Equal(2, reply.Recipients);
        Assert.Equal([42L, 402763435L], ChatIds().OrderBy(id => id).ToArray());
        Assert.All(Texts(), text => Assert.Contains("ссылка на авторизацию", text));
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
    /// Один недоступный администратор не должен лишать остальных сообщения:
    /// доставка идёт по каждому отдельно.
    /// </summary>
    [Fact]
    public async Task Ошибка_У_Одного_Не_Срывает_Остальных()
    {
        FailForChat(2);

        var reply = await CaptureAsync(
            [1, 2, 3],
            client => client.SendToAdminsAsync(Request("ссылка"))
        );

        Assert.Equal(2, reply.Recipients);
    }

    /// <summary>
    /// А вот когда не доставил никому — это уже ошибка доставки, и вызывающий
    /// должен узнать о ней статусом, иначе просьба считалась бы выполненной.
    /// </summary>
    [Fact]
    public async Task Когда_Не_Доставил_Никому_Просьба_Падает()
    {
        FailForChat(1);
        FailForChat(2);

        var status = await StatusAsync(
            [1, 2],
            client => client.SendToAdminsAsync(Request("ссылка"))
        );

        Assert.Equal(StatusCode.Unavailable, status.StatusCode);
    }

    private AdminMessageRequest Request(string message) =>
        new() { Service = "MARS.TwitchCore", Message = message };

    private long[] ChatIds() =>
        _bot
            .Invocations.Select(invocation => invocation.Arguments[0])
            .OfType<SendMessageRequest>()
            .Select(request => request.ChatId.Identifier ?? 0)
            .ToArray();

    private string[] Texts() =>
        _bot
            .Invocations.Select(invocation => invocation.Arguments[0])
            .OfType<SendMessageRequest>()
            .Select(request => request.Text)
            .ToArray();

    /// <summary>
    /// Telegram отказал именно этому чату: у удалённого администратора так и будет.
    /// </summary>
    private void FailForChat(long chatId) =>
        _bot.Setup(bot =>
                bot.SendRequest(
                    It.Is<SendMessageRequest>(request => request.ChatId.Identifier == chatId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ApiRequestException("chat not found", 400));

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
        _bot.Invocations.Clear();

        var options = Options.Create(new TelegramConfiguration { AdminIds = admins });

        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(
            new TelegramAdminGrpcService(
                withBot ? _bot.Object : null,
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
