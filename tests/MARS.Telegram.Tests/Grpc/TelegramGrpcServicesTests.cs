using Grpc.Core;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Shared.Contracts.TelegramLog;
using MARS.Shared.Contracts.TelegramMessage;
using MARS.Telegram.Api.Rest.Grpc;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Logging;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MARS.Telegram.Tests.Grpc;

public class TelegramGrpcServicesTests
{
    private const long Admin = 402763435;
    private const long LogChat = -1001234567890;

    private readonly Mock<ITelegramMessageSender> _sender = new();
    private readonly Mock<ServerCallContext> _context = new();

    public TelegramGrpcServicesTests()
    {
        _context
            .Protected()
            .SetupGet<CancellationToken>("CancellationTokenCore")
            .Returns(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AdminMessageReturnsRecipientCount()
    {
        _sender
            .Setup(sender => sender.SendToAdminsAsync("текст", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var reply = await AdminService([Admin, 42])
            .SendToAdmins(
                new AdminMessageRequest { Service = "MARS.TwitchCore", Message = "текст" },
                _context.Object
            );

        Assert.Equal(2, reply.Recipients);
    }

    [Theory]
    [InlineData(StatusCode.FailedPrecondition)]
    [InlineData(StatusCode.InvalidArgument)]
    [InlineData(StatusCode.Unavailable)]
    public async Task AdminFailuresUseGrpcStatus(StatusCode expected)
    {
        var botToken = expected == StatusCode.FailedPrecondition ? string.Empty : "token";
        var adminIds = expected == StatusCode.FailedPrecondition ? new long[] { Admin } : [];
        var message = expected == StatusCode.InvalidArgument ? " " : "текст";
        if (expected == StatusCode.Unavailable)
        {
            adminIds = [Admin];
            _sender
                .Setup(sender => sender.SendToAdminsAsync(message, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
        }

        var error = await Assert.ThrowsAsync<RpcException>(() =>
            AdminService(adminIds, botToken)
                .SendToAdmins(new AdminMessageRequest { Message = message }, _context.Object)
        );

        Assert.Equal(expected, error.StatusCode);
    }

    [Fact]
    public async Task LogEntryIsFormattedAndDelivered()
    {
        string? captured = null;
        IReadOnlyList<long>? chats = null;
        _sender
            .Setup(sender =>
                sender.SendPlainToChatsAsync(
                    It.IsAny<IReadOnlyList<long>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IReadOnlyList<long>, string, CancellationToken>(
                (targets, text, _) =>
                {
                    chats = targets;
                    captured = text;
                }
            )
            .ReturnsAsync(1);

        var reply = await LogService()
            .Publish(
                new LogEntryRequest
                {
                    Service = "MARS.TwitchCore",
                    Level = "Error",
                    Category = "MARS.TwitchCore.Services.Rewards",
                    Message = "Что-то сломалось",
                },
                _context.Object
            );

        Assert.Equal(1, reply.Recipients);
        Assert.Equal(new long[] { LogChat }, chats);
        Assert.Contains("MARS.TwitchCore", captured);
        Assert.Contains("Что-то сломалось", captured);
    }

    [Fact]
    public async Task EmptyLogChatListFallsBackToAdmins()
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
            .Callback<IReadOnlyList<long>, string, CancellationToken>(
                (targets, _, _) => chats = targets
            )
            .ReturnsAsync(1);

        await LogService(logChatIds: [])
            .Publish(
                new LogEntryRequest { Service = "MARS.TwitchCore", Message = "текст" },
                _context.Object
            );

        Assert.Equal(new long[] { Admin }, chats);
    }

    [Theory]
    [InlineData(StatusCode.InvalidArgument)]
    [InlineData(StatusCode.FailedPrecondition)]
    [InlineData(StatusCode.Unavailable)]
    public async Task LogFailuresUseGrpcStatus(StatusCode expected)
    {
        var botToken = expected == StatusCode.FailedPrecondition ? string.Empty : "token";
        var request = new LogEntryRequest { Service = "MARS.TwitchCore", Message = "текст" };
        if (expected == StatusCode.InvalidArgument)
        {
            request.Service = " ";
        }
        else if (expected == StatusCode.Unavailable)
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
        }

        var error = await Assert.ThrowsAsync<RpcException>(() =>
            LogService(botToken: botToken).Publish(request, _context.Object)
        );

        Assert.Equal(expected, error.StatusCode);
    }

    [Fact]
    public async Task LogRateLimitReturnsSuppressedCount()
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
        var service = LogService(rateLimitPerMinute: 1);
        var request = new LogEntryRequest { Service = "MARS.TwitchCore", Message = "текст" };

        await service.Publish(request, _context.Object);
        var second = await service.Publish(request, _context.Object);

        Assert.Equal(0, second.Recipients);
        Assert.Equal(1, second.Suppressed);
    }

    [Fact]
    public async Task MessageDeliveryReturnsTypedReply()
    {
        _sender
            .Setup(sender => sender.SendAsync(123, "текст", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var reply = await MessageService()
            .Send(new SendMessageRequest { ChatId = 123, Text = "текст" }, _context.Object);

        Assert.True(reply.Delivered);
    }

    [Theory]
    [InlineData(StatusCode.InvalidArgument)]
    [InlineData(StatusCode.FailedPrecondition)]
    [InlineData(StatusCode.Unavailable)]
    public async Task MessageFailuresUseGrpcStatus(StatusCode expected)
    {
        var botToken = expected == StatusCode.FailedPrecondition ? string.Empty : "token";
        var text = expected == StatusCode.InvalidArgument ? " " : "текст";
        if (expected == StatusCode.Unavailable)
        {
            _sender
                .Setup(sender => sender.SendAsync(123, text, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        var error = await Assert.ThrowsAsync<RpcException>(() =>
            MessageService(botToken)
                .Send(new SendMessageRequest { ChatId = 123, Text = text }, _context.Object)
        );

        Assert.Equal(expected, error.StatusCode);
    }

    private TelegramAdminGrpcService AdminService(
        long[]? adminIds = null,
        string botToken = "token"
    ) =>
        new(
            _sender.Object,
            Options(botToken, adminIds ?? [Admin]),
            NullLogger<TelegramAdminGrpcService>.Instance
        );

    private TelegramLogGrpcService LogService(
        string botToken = "token",
        long[]? logChatIds = null,
        int rateLimitPerMinute = TelegramLogRateLimiter.DefaultLimitPerMinute
    ) =>
        new(
            _sender.Object,
            Options(botToken, logChatIds: logChatIds ?? [LogChat]),
            new TelegramLogRateLimiter(rateLimitPerMinute),
            NullLogger<TelegramLogGrpcService>.Instance
        );

    private TelegramMessageGrpcService MessageService(string botToken = "token") =>
        new(
            _sender.Object,
            Mock.Of<IGooglePhotosAuthService>(),
            Mock.Of<IRandomMemService>(),
            Mock.Of<IWTelegramClientService>(),
            Mock.Of<ITelegramWhitelistService>(),
            Options(botToken),
            NullLogger<TelegramMessageGrpcService>.Instance
        );

    private static IOptions<TelegramConfiguration> Options(
        string botToken,
        long[]? adminIds = null,
        long[]? logChatIds = null
    ) =>
        Microsoft.Extensions.Options.Options.Create(
            new TelegramConfiguration
            {
                BotToken = botToken,
                AdminIds = adminIds ?? [Admin],
                LogChatIds = logChatIds ?? [LogChat],
            }
        );
}
