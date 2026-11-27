using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Telegram;
using MARS.Shared.Kernel;
using MARS.Telegram.Api.Rest.Controllers;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Telegram.Tests.Controllers;

public class TelegramAlertControllerTests
{
    private const long Admin = 402763435;
    private const long LogChat = -1001234567890;

    private readonly Mock<ITelegramMessageSender> _sender = new();

    [Fact]
    public async Task AlertReturnsRecipientCount()
    {
        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(2);

        var result = Ok(
            await Controller().PublishAlert(Request(), TestContext.Current.CancellationToken)
        );

        Assert.Equal(2, result.Result!.Recipients);
    }

    [Fact]
    public async Task MissingBotTokenIsReportedAsPrecondition()
    {
        Failed(
            await Controller(botToken: string.Empty)
                .PublishAlert(Request(), TestContext.Current.CancellationToken),
            HttpStatusCode.PreconditionFailed
        );

        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingChatsAreReportedAsPrecondition()
    {
        Failed(
            await Controller(adminIds: [], logChatIds: [])
                .PublishAlert(Request(), TestContext.Current.CancellationToken),
            HttpStatusCode.PreconditionFailed
        );

        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EmptyAlertIsRejected()
    {
        BadAlert(
            await Controller()
                .PublishAlert(
                    new TelegramAlertWebhookRequest(),
                    TestContext.Current.CancellationToken
                )
        );
    }

    [Fact]
    public async Task DeliveryFailureRemainsRetryable()
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
            await Controller().PublishAlert(Request(), TestContext.Current.CancellationToken),
            HttpStatusCode.ServiceUnavailable
        );
    }

    private TelegramAlertController Controller(
        string botToken = "token",
        long[]? adminIds = null,
        long[]? logChatIds = null
    ) =>
        new(
            _sender.Object,
            Options.Create(
                new TelegramConfiguration
                {
                    BotToken = botToken,
                    AdminIds = adminIds ?? [Admin],
                    LogChatIds = logChatIds ?? [LogChat],
                }
            ),
            NullLogger<TelegramAlertController>.Instance
        );

    private static TelegramAlertWebhookRequest Request() =>
        new()
        {
            Status = "firing",
            Alerts =
            [
                new TelegramAlertWebhookItem
                {
                    Status = "firing",
                    Labels = new Dictionary<string, string>
                    {
                        ["alertname"] = "MarsLogError",
                        ["service"] = "twitch-core",
                    },
                    Annotations = new Dictionary<string, string>
                    {
                        ["summary"] = "Ошибки в журнале",
                    },
                },
            ],
        };

    private static OperationResult<TelegramAlertResult> Ok(
        ActionResult<OperationResult<TelegramAlertResult>> action
    ) =>
        Assert.IsType<OperationResult<TelegramAlertResult>>(
            Assert.IsType<OkObjectResult>(action.Result).Value
        );

    private static OperationResult<TelegramAlertResult> Failed(
        ActionResult<OperationResult<TelegramAlertResult>> action,
        HttpStatusCode expected
    )
    {
        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal((int)expected, result.StatusCode);
        return Assert.IsType<OperationResult<TelegramAlertResult>>(result.Value);
    }

    private static void BadAlert(ActionResult<OperationResult<TelegramAlertResult>> action) =>
        Assert.IsType<OperationResult<TelegramAlertResult>>(
            Assert.IsType<BadRequestObjectResult>(action.Result).Value
        );
}
