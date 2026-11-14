using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Telegram;
using MARS.Shared.Kernel;
using MARS.Telegram.Api.Rest.Controllers;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Logging;
using MARS.Telegram.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Telegram.Tests.Controllers;

/// <summary>
/// Внутренние REST-эндпоинты MARS.Telegram: доставка сообщения администраторам и
/// приём записи журнала.
/// </summary>
/// <remarks>
/// Проверяется то, что видит вызывающий сервис: успешный конверт, различие
/// статусов «не настроено» (412, повторять бессмысленно) и «не доставлено»
/// (503, повторять имеет смысл) и отказ на пустом запросе (400). Отправитель —
/// заглушка: настоящий ходит в Telegram, и юнит-тест, проверяющий сеть,
/// проверял бы сеть.
/// </remarks>
public class TelegramInternalControllerTests
{
    private const long Admin = 402763435;
    private const long LogChat = -1001234567890;

    private readonly Mock<ITelegramMessageSender> _sender = new();

    [Fact]
    public async Task Сообщение_Администраторам_Возвращает_Число_Получателей()
    {
        _sender
            .Setup(sender =>
                sender.SendToAdminsAsync("ссылка на авторизацию", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(2);

        var result = Ok(
            await Controller(adminIds: [Admin, 42])
                .SendToAdmins(
                    new TelegramAdminMessageRequest
                    {
                        Service = "MARS.TwitchCore",
                        Message = "ссылка на авторизацию",
                    },
                    TestContext.Current.CancellationToken
                )
        );

        Assert.Equal(2, result.Result!.Recipients);
    }

    [Fact]
    public async Task Без_Токена_Бота_Сообщение_Отклоняется_Статусом_412()
    {
        var result = Failed(
            await Controller(botToken: string.Empty)
                .SendToAdmins(
                    new TelegramAdminMessageRequest { Message = "текст" },
                    TestContext.Current.CancellationToken
                ),
            HttpStatusCode.PreconditionFailed
        );

        Assert.Contains("BotToken", result.ErrorMessage);
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Пустой_Список_Администраторов_Отклоняется_Статусом_412()
    {
        var result = Failed(
            await Controller(adminIds: [])
                .SendToAdmins(
                    new TelegramAdminMessageRequest { Message = "текст" },
                    TestContext.Current.CancellationToken
                ),
            HttpStatusCode.PreconditionFailed
        );

        Assert.Contains("AdminIds", result.ErrorMessage);
    }

    [Fact]
    public async Task Пустое_Сообщение_Отклоняется_Статусом_400()
    {
        var result = Bad(
            await Controller()
                .SendToAdmins(
                    new TelegramAdminMessageRequest { Message = "  " },
                    TestContext.Current.CancellationToken
                )
        );

        Assert.False(result.Success);
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Недоставленное_Сообщение_Возвращает_Статус_503()
    {
        _sender
            .Setup(sender =>
                sender.SendToAdminsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(0);

        var result = Failed(
            await Controller()
                .SendToAdmins(
                    new TelegramAdminMessageRequest { Message = "текст" },
                    TestContext.Current.CancellationToken
                ),
            HttpStatusCode.ServiceUnavailable
        );

        Assert.False(result.Success);
    }

    /// <summary>
    /// Запись уходит текстом в чаты журнала, а текст собирает общий форматтер:
    /// в нём есть уровень, сервис и само сообщение.
    /// </summary>
    [Fact]
    public async Task Запись_Журнала_Уходит_В_Чаты_Журнала()
    {
        CapturedText? captured = null;

        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IReadOnlyList<long>, string, CancellationToken>(
                (chats, text, _) => captured = new CapturedText(chats, text)
            )
            .ReturnsAsync(1);

        var result = Ok(
            await Controller()
                .PublishLogEntry(
                    new TelegramLogEntryRequest
                    {
                        Service = "MARS.TwitchCore",
                        Level = "Error",
                        Category = "MARS.TwitchCore.Services.Rewards",
                        Message = "Что-то сломалось",
                    },
                    TestContext.Current.CancellationToken
                )
        );

        Assert.Equal(1, result.Result!.Recipients);
        Assert.NotNull(captured);
        Assert.Equal(new long[] { LogChat }, captured!.Chats);
        Assert.Contains("MARS.TwitchCore", captured.Text);
        Assert.Contains("Что-то сломалось", captured.Text);
    }

    /// <summary>
    /// Без отдельного списка чатов журнал идёт администраторам: отдельный чат
    /// завести забывают чаще, чем он нужен, а остаться без журнала хуже.
    /// </summary>
    [Fact]
    public async Task Без_Чатов_Журнала_Запись_Идёт_Администраторам()
    {
        IReadOnlyList<long>? chats = null;

        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IReadOnlyList<long>, string, CancellationToken>((sent, _, _) => chats = sent)
            .ReturnsAsync(1);

        await Controller(logChatIds: [])
            .PublishLogEntry(
                new TelegramLogEntryRequest { Service = "MARS.TwitchCore", Message = "текст" },
                TestContext.Current.CancellationToken
            );

        Assert.NotNull(chats);
        Assert.Equal(new long[] { Admin }, chats!);
    }

    [Fact]
    public async Task Запись_Без_Сервиса_Отклоняется_Статусом_400()
    {
        Bad(
            await Controller()
                .PublishLogEntry(
                    new TelegramLogEntryRequest { Service = " ", Message = "текст" },
                    TestContext.Current.CancellationToken
                )
        );
    }

    [Fact]
    public async Task Пустая_Запись_Отклоняется_Статусом_400()
    {
        Bad(
            await Controller()
                .PublishLogEntry(
                    new TelegramLogEntryRequest { Service = "MARS.TwitchCore" },
                    TestContext.Current.CancellationToken
                )
        );
    }

    [Fact]
    public async Task Запись_При_Ненастроенном_Боте_Отклоняется_Статусом_412()
    {
        Failed(
            await Controller(botToken: string.Empty)
                .PublishLogEntry(
                    new TelegramLogEntryRequest { Service = "MARS.TwitchCore", Message = "текст" },
                    TestContext.Current.CancellationToken
                ),
            HttpStatusCode.PreconditionFailed
        );
    }

    [Fact]
    public async Task Недоставленная_Запись_Возвращает_Статус_503()
    {
        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(0);

        Failed(
            await Controller()
                .PublishLogEntry(
                    new TelegramLogEntryRequest { Service = "MARS.TwitchCore", Message = "текст" },
                    TestContext.Current.CancellationToken
                ),
            HttpStatusCode.ServiceUnavailable
        );
    }

    /// <summary>
    /// Лимит частоты считается на сервис-отправителя: вторая запись того же
    /// сервиса в окне отбрасывается, но вызывающий получает успешный конверт с
    /// числом подавленных записей, а не ошибку.
    /// </summary>
    [Fact]
    public async Task Запись_Сверх_Лимита_Подавляется_Счётчиком()
    {
        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(1);

        var controller = Controller(rateLimitPerMinute: 1);
        var request = new TelegramLogEntryRequest
        {
            Service = "MARS.TwitchCore",
            Message = "текст",
        };

        Ok(await controller.PublishLogEntry(request, TestContext.Current.CancellationToken));

        var second = Ok(
            await controller.PublishLogEntry(request, TestContext.Current.CancellationToken)
        );

        Assert.Equal(0, second.Result!.Recipients);
        Assert.Equal(1, second.Result.Suppressed);
    }

    private TelegramInternalController Controller(
        string botToken = "token",
        long[]? adminIds = null,
        long[]? logChatIds = null,
        int rateLimitPerMinute = TelegramLogRateLimiter.DefaultLimitPerMinute
    )
    {
        var configuration = Options.Create(
            new TelegramConfiguration
            {
                BotToken = botToken,
                AdminIds = adminIds ?? [Admin],
                LogChatIds = logChatIds ?? [LogChat],
            }
        );

        return new TelegramInternalController(
            _sender.Object,
            configuration,
            new TelegramLogRateLimiter(rateLimitPerMinute),
            NullLogger<TelegramInternalController>.Instance
        );
    }

    private static OperationResult<TelegramAdminMessageResult> Ok(
        ActionResult<OperationResult<TelegramAdminMessageResult>> action
    ) =>
        Assert.IsType<OperationResult<TelegramAdminMessageResult>>(
            Assert.IsType<OkObjectResult>(action.Result).Value
        );

    private static OperationResult<TelegramLogEntryResult> Ok(
        ActionResult<OperationResult<TelegramLogEntryResult>> action
    ) =>
        Assert.IsType<OperationResult<TelegramLogEntryResult>>(
            Assert.IsType<OkObjectResult>(action.Result).Value
        );

    private static OperationResult<TelegramAdminMessageResult> Failed(
        ActionResult<OperationResult<TelegramAdminMessageResult>> action,
        HttpStatusCode expected
    )
    {
        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal((int)expected, result.StatusCode);

        return Assert.IsType<OperationResult<TelegramAdminMessageResult>>(result.Value);
    }

    private static OperationResult<TelegramLogEntryResult> Failed(
        ActionResult<OperationResult<TelegramLogEntryResult>> action,
        HttpStatusCode expected
    )
    {
        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal((int)expected, result.StatusCode);

        return Assert.IsType<OperationResult<TelegramLogEntryResult>>(result.Value);
    }

    private static OperationResult<TelegramAdminMessageResult> Bad(
        ActionResult<OperationResult<TelegramAdminMessageResult>> action
    ) =>
        Assert.IsType<OperationResult<TelegramAdminMessageResult>>(
            Assert.IsType<BadRequestObjectResult>(action.Result).Value
        );

    private static OperationResult<TelegramLogEntryResult> Bad(
        ActionResult<OperationResult<TelegramLogEntryResult>> action
    ) =>
        Assert.IsType<OperationResult<TelegramLogEntryResult>>(
            Assert.IsType<BadRequestObjectResult>(action.Result).Value
        );

    private sealed record CapturedText(IReadOnlyList<long> Chats, string Text);
}
