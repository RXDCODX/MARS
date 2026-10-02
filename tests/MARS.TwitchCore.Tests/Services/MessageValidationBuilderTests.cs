using System.Collections.Concurrent;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Проверки команд чата: каждая складывается из требований, и каждое требование
/// либо проходит молча, либо объясняет пользователю причину.
///
/// Разница между громким и тихим требованием — не стилистика, а смысл: тихое
/// молчит и помечает проверку как проваленную, чтобы последующие громкие
/// требования не добавили второго сообщения в чат.
/// </summary>
public class MessageValidationBuilderTests
{
    [Fact]
    public async Task ChatMessageFromOtherChannelIsRejected()
    {
        var builder = Builder(Channel: "чужой_канал");

        var result = await builder.RequireChannel().ValidateAsync();

        // IsValid при тихой неудаче тоже false: команда не выполнится, но и
        // сообщения в чате не будет — именно этим тихая проверка отличается.
        Assert.True(result.HasSilentFailure);
        Assert.True(result.IsInvalid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ChatMessageFromMainChannelPasses()
    {
        var builder = Builder(Channel: MARS.TwitchCore.Extensions.TwitchConstants.Channel);

        var result = await builder.RequireChannel().ValidateAsync();

        Assert.True(result.IsValid);
        Assert.False(result.HasSilentFailure);
    }

    [Fact]
    public async Task BroadcasterCheckFailsForViewer()
    {
        var builder = Builder(RoomId: "viewer-room");

        var result = await builder.RequireBroadcasterId(loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("стримеру", result.FirstError);
    }

    [Fact]
    public async Task UnknownRewardIsRejected()
    {
        var builder = Builder(CustomRewardId: null);

        var result = await builder.RequireRewardId(loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("награду", result.FirstError);
    }

    [Fact]
    public async Task RewardGuidMismatchIsRejected()
    {
        var builder = Builder(CustomRewardId: Guid.NewGuid().ToString());

        var result = await builder.RequireRewardGuid(Guid.NewGuid(), loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("не найдена", result.FirstError);
    }

    /// <summary>
    /// Отсутствие guid в настройке — это ошибка конфигурации, а не отказ
    /// пользователя: команда не должна работать, но обязана сказать об этом
    /// вслух.
    /// </summary>
    [Fact]
    public async Task MissingConfiguredGuidIsLoudFailure()
    {
        var builder = Builder(CustomRewardId: Guid.NewGuid().ToString());

        var result = await builder.RequireRewardGuid(expected: null, loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("не настроена", result.FirstError);
    }

    /// <summary>
    /// Совпадение guid проверить нельзя: TwitchLib не даёт задать
    /// CustomRewardId вне конструктора, который его вообще не принимает. Проверка
    /// на несовпадение и на ненастроенную награду покрывают обе ветви, кроме
    /// успешной.
    /// </summary>
    [Fact]
    public async Task InactiveServiceIsRejected()
    {
        var builder = Builder();

        var result = await builder
            .RequireServiceActive(isActive: false, loud: true)
            .ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("неактивен", result.FirstError);
    }

    [Fact]
    public async Task MissingUserIsRejected()
    {
        var builder = Builder(UserId: null);

        var result = await builder.RequireUserId(loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("пользователя", result.FirstError);
    }

    /// <summary>
    /// Тихая неудача отсекает следующее громкое требование: иначе в чат ушло бы
    /// два сообщения об ошибке от одной команды.
    /// </summary>
    [Fact]
    public async Task SilentFailureSuppressesLaterLoudErrors()
    {
        var builder = Builder(Channel: "чужой_канал", UserId: null);

        var result = await builder.RequireChannel().RequireUserId(loud: true).ValidateAsync();

        Assert.True(result.HasSilentFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidationErrorIsSentToTheChannelOnce()
    {
        var client = new Mock<ITwitchClient>();
        var sent = new ConcurrentDictionary<string, DateTime>();
        var builder = new MessageValidationBuilder(
            Args(RoomId: "viewer-room"),
            client.Object,
            NullLogger.Instance,
            sent
        );

        await builder.RequireBroadcasterId(loud: true).ValidateWithResponseAsync("viewer");

        await builder.RequireBroadcasterId(loud: true).ValidateWithResponseAsync("viewer");

        client.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<string>(),
                    It.Is<string>(message => message.Contains("viewer")),
                    It.IsAny<bool>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task FailedSendDoesNotBreakValidation()
    {
        var client = new Mock<ITwitchClient>();
        client
            .Setup(instance =>
                instance.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())
            )
            .ThrowsAsync(new InvalidOperationException("чат недоступен"));
        var builder = new MessageValidationBuilder(
            Args(RoomId: "viewer-room"),
            client.Object,
            NullLogger.Instance,
            new ConcurrentDictionary<string, DateTime>()
        );

        var result = await builder
            .RequireBroadcasterId(loud: true)
            .ValidateWithResponseAsync("viewer");

        Assert.True(result.IsInvalid);
    }

    private static MessageValidationBuilder Builder(
        string Channel = "канал",
        string? RoomId = "room",
        string? UserId = "user",
        string? CustomRewardId = "reward"
    ) =>
        new(
            Args(Channel, RoomId, UserId, CustomRewardId),
            Mock.Of<ITwitchClient>(),
            NullLogger.Instance,
            new ConcurrentDictionary<string, DateTime>()
        );

    /// <summary>
    /// Сообщение TwitchLib собирается конструктором на двадцать два параметра и
    /// отдаёт readonly-свойства, а <c>CustomRewardId</c> в конструкторе нет вовсе
    /// (он выводится из бейджей). Поэтому сообщение строится конструктором, а
    /// идентификатор награды ставится рефлексией в приватный сеттер: проверяем
    /// мы проверки команд, а не разбор IRC-сообщения.
    /// </summary>
    private static OnMessageReceivedArgs Args(
        string Channel = "канал",
        string? RoomId = "room",
        string? UserId = "user",
        string? CustomRewardId = "reward"
    )
    {
        var message = new ChatMessage(
            botUsername: "mars-bot",
            userId: UserId ?? string.Empty,
            userName: "pyro",
            displayName: "pyro",
            hexColor: "#ffffff",
            emoteSet: null!,
            message: "текст",
            userType: UserType.Viewer,
            channel: Channel,
            id: "message-1",
            subscribedMonthCount: 0,
            roomId: RoomId ?? string.Empty,
            isMe: false,
            isBroadcaster: false,
            noisy: default(Noisy),
            rawIrcMessage: string.Empty,
            emoteReplacedMessage: string.Empty,
            badges: Badges(CustomRewardId),
            cheerBadge: null!,
            bits: 0,
            bitsInDollars: 0,
            userDetail: default(UserDetail)
        );

        return new OnMessageReceivedArgs(message);
    }

    /// <summary>
    /// Идентификатор награды TwitchLib выводит из бейджа «custom-reward»,
    /// отдельного сеттера у него нет — отсюда и конструктор на двадцать два
    /// параметра.
    /// </summary>
    private static List<KeyValuePair<string, string>> Badges(string? customRewardId) =>
        customRewardId is null
            ? []
            : [new KeyValuePair<string, string>("custom-reward-id", customRewardId)];
}
