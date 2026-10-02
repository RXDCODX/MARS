using System.Collections.Concurrent;
using System.Reflection;
using MARS.Shared.Clients;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.TekkenStreams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Пересылка сообщений из чатов теккен-стримов в Discord.
///
/// Проверяется, что пересылается только то, что должно: сообщения своего канала
/// и чатов, которых нет в списке стримов, остаются в Twitch, а настроенный
/// пересылается с указанием канала. Без этого в Discord попадал бы весь чат,
/// включая чужие каналы.
///
/// Список подключённых каналов и id Discord-канала — приватные поля: они
/// заполняются синхронизацией, которая уходит в Twitch. Поэтому в тесте они
/// выставляются напрямую, и проверяется именно решение о пересылке.
/// </summary>
public class TekkenStreamsDiscordForwarderServiceTests
{
    private const ulong DiscordChannelId = 1406679380369080481;

    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly Mock<ITwitchClient> _twitch = new();
    private readonly Mock<IDiscordClient> _discord = new();
    private readonly TekkenStreamsDiscordForwarderService _service;

    public TekkenStreamsDiscordForwarderServiceTests()
    {
        _discord
            .Setup(instance =>
                instance.SendMessageAsync(
                    It.IsAny<ulong>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        _service = new TekkenStreamsDiscordForwarderService(
            _twitch.Object,
            Mock.Of<ITwitchAPI>(),
            _discord.Object,
            _factory,
            NullLogger<TekkenStreamsDiscordForwarderService>.Instance
        );
    }

    /// <summary>
    /// Без настройки в <c>RootState</c> пересылка не настроена, но сервис не падает:
    /// обход стримов продолжается, а сообщения просто не уходят в Discord.
    /// </summary>
    [Fact]
    public async Task SyncWithoutConfigurationDoesNotThrow()
    {
        await _service.SyncStreamsAsync(TestContext.Current.CancellationToken);

        _twitch.Verify(instance => instance.JoinChannelAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SyncWithConfiguredChannelDoesNotThrow()
    {
        await SeedDiscordChannelAsync(DiscordChannelId);

        await _service.SyncStreamsAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Свои сообщения на свой канал не пересылаются: иначе Discord получал бы
    /// эхо всего чата бота.
    /// </summary>
    [Fact]
    public async Task OwnChannelIsNotForwarded()
    {
        ConfigureForwarding(TwitchConstants.Channel);

        await _service.OnMessageReceived(null, Message(TwitchConstants.Channel));

        _discord.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<ulong>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task UnknownChannelIsNotForwarded()
    {
        ConfigureForwarding(TwitchConstants.Channel);

        await _service.OnMessageReceived(null, Message("чужой-канал"));

        _discord.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<ulong>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// Настроенный канал стрима пересылается с указанием канала, иначе в Discord
    /// не разобрать, откуда пришло сообщение.
    /// </summary>
    [Fact]
    public async Task SyncedChannelIsForwarded()
    {
        ConfigureForwarding("tekken-streamer");

        await _service.OnMessageReceived(null, Message("tekken-streamer"));

        _discord.Verify(
            instance =>
                instance.SendMessageAsync(
                    DiscordChannelId,
                    "[tekken-streamer] Pyro: привет",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Отказ Discord не поднимается наружу: обработчик события не должен ронять
    /// чтение чата.
    /// </summary>
    [Fact]
    public async Task DiscordFailureDoesNotThrow()
    {
        ConfigureForwarding("tekken-streamer");
        _discord
            .Setup(instance =>
                instance.SendMessageAsync(
                    It.IsAny<ulong>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);

        await _service.OnMessageReceived(null, Message("tekken-streamer"));
    }

    [Fact]
    public async Task StopWithoutStartIsSafe()
    {
        await _service.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Заполняет приватное состояние так же, как это делает синхронизация:
    /// подключённый канал и id Discord-канала из <c>RootState</c>.
    /// </summary>
    private void ConfigureForwarding(params string[] tekkenChannels)
    {
        SetField(
            "_tekkenChannels",
            new ConcurrentDictionary<string, byte>(
                tekkenChannels.Select(channel => new KeyValuePair<string, byte>(channel, 0)),
                StringComparer.OrdinalIgnoreCase
            )
        );
        SetField("_discordChannelId", DiscordChannelId);
    }

    private void SetField(string name, object value) =>
        typeof(TekkenStreamsDiscordForwarderService)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_service, value);

    private async Task SeedDiscordChannelAsync(ulong channelId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.TekkenStreamsDiscordChannelId,
                Value = channelId.ToString(),
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static OnMessageReceivedArgs Message(string channel) =>
        new(
            new ChatMessage(
                botUsername: "mars-bot",
                userId: "123456789",
                userName: "pyro",
                displayName: "Pyro",
                hexColor: "#FFFFFF",
                emoteSet: null!,
                message: "привет",
                userType: UserType.Viewer,
                channel: channel,
                id: "message-1",
                subscribedMonthCount: 0,
                roomId: "room",
                isMe: false,
                isBroadcaster: false,
                noisy: default,
                rawIrcMessage: string.Empty,
                emoteReplacedMessage: string.Empty,
                badges: [],
                cheerBadge: null!,
                bits: 0,
                bitsInDollars: 0,
                userDetail: default
            )
        );

    /// <summary>
    /// Фоновый цикл подписывается на сообщения чата и отписывается при остановке:
    /// без отписки выключенный сервис продолжал бы пересылать чат в Discord.
    /// </summary>
    [Fact]
    public async Task LoopSubscribesAndUnsubscribesOnStop()
    {
        using var stopping = new CancellationTokenSource();
        stopping.CancelAfter(TimeSpan.FromMilliseconds(200));

        // Цикл живёт до отмены токена: сам PeriodicTimer при отмене бросает
        // TaskCanceledException, и это ожидаемое завершение фоновой задачи.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeExecuteAsync(stopping.Token)
        );

        // Обработчик снимается в finally, поэтому после выхода подписки быть не
        // должно — иначе сервис пересылал бы чат после остановки.
        _twitch.VerifyRemove(
            instance =>
                instance.OnMessageReceived -= It.IsAny<AsyncEventHandler<OnMessageReceivedArgs>>(),
            Times.Once
        );
    }

    /// <summary>
    /// Цикл синхронизации успевает снять подписку даже при отмене сразу: иначе
    /// подписка жила бы до перезапуска процесса.
    /// </summary>
    [Fact]
    public async Task LoopWithCancelledTokenStopsImmediately()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        // Цикл живёт до отмены токена: сам PeriodicTimer при отмене бросает
        // TaskCanceledException, и это ожидаемое завершение фоновой задачи.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeExecuteAsync(stopping.Token)
        );

        _twitch.VerifyRemove(
            instance =>
                instance.OnMessageReceived -= It.IsAny<AsyncEventHandler<OnMessageReceivedArgs>>(),
            Times.Once
        );
    }

    private Task InvokeExecuteAsync(CancellationToken stoppingToken)
    {
        var method = typeof(TekkenStreamsDiscordForwarderService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(_service, [stoppingToken])!;
    }
}
