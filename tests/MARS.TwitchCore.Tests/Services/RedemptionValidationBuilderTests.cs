using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.Models.ChannelPoints;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Проверки наград канала. Каждая складывается из требований, и каждое
/// требование либо проходит, либо объясняет пользователю причину.
///
/// Разница между громким и тихим требованием — не стилистика: тихое молчит и
/// помечает проверку как проваленную, чтобы следующее громкое не добавило в чат
/// второго сообщения.
/// </summary>
public class RedemptionValidationBuilderTests
{
    [Fact]
    public async Task RedemptionFromMainChannelPassesEveryCheck()
    {
        var builder = Builder(
            BroadcasterUserId: TwitchConstants.ChannelId,
            BroadcasterUserLogin: TwitchConstants.Channel,
            Cost: 100
        );

        var result = await builder
            .RequireBroadcasterUserId()
            .RequireBroadcasterUserLogin()
            .RequireCost(100)
            .RequireServiceActive(true)
            .RequireRewardEnabled(() => true)
            .RequireRewardGuid(RewardId)
            .ValidateAsync();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.False(result.HasSilentFailure);
    }

    [Fact]
    public async Task ForeignChannelIdIsRejected()
    {
        var builder = Builder(BroadcasterUserId: "999");

        var result = await builder.RequireBroadcasterUserId(loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("основном канале", result.FirstError);
    }

    [Fact]
    public async Task MissingPayloadIsRejected()
    {
        var builder = new RedemptionValidationBuilder(
            new ChannelPointsCustomRewardRedemptionArgs(),
            Mock.Of<ITwitchClient>(),
            NullLogger.Instance
        );

        var result = await builder.RequireBroadcasterUserId(loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("основном канале", result.FirstError);
    }

    [Fact]
    public async Task ForeignChannelLoginIsRejected()
    {
        var builder = Builder(BroadcasterUserLogin: "чужой_канал");

        var result = await builder.RequireBroadcasterUserLogin(loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("основном канале", result.FirstError);
    }

    [Fact]
    public async Task ChannelLoginComparisonIgnoresCase()
    {
        var builder = Builder(BroadcasterUserLogin: TwitchConstants.Channel.ToUpperInvariant());

        var result = await builder.RequireBroadcasterUserLogin().ValidateAsync();

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task WrongCostIsRejected()
    {
        var builder = Builder(Cost: 50);

        var result = await builder.RequireCost(100, loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("стоимость", result.FirstError);
    }

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
    public async Task DisabledRewardIsRejected()
    {
        var builder = Builder();

        var result = await builder.RequireRewardEnabled(() => false, loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("отключена", result.FirstError);
    }

    [Fact]
    public async Task UnconfiguredRewardIdIsRejected()
    {
        var builder = Builder();

        var result = await builder.RequireRewardGuid(null, loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("не настроена", result.FirstError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("не guid")]
    [InlineData("99999999-8888-7777-6666-555555555555")]
    public async Task MismatchedRewardIdIsRejected(string rewardId)
    {
        var builder = Builder(RewardIdText: rewardId);

        var result = await builder.RequireRewardGuid(RewardId, loud: true).ValidateAsync();

        Assert.True(result.IsInvalid);
        Assert.Contains("не найдена", result.FirstError);
    }

    /// <summary>
    /// Тихая проверка не пишет в чат, но помечает результат как невалидный:
    /// команда не выполнится, и в чате будет тихо.
    /// </summary>
    [Fact]
    public async Task SilentFailureProducesNoErrorsButFailsValidation()
    {
        var builder = Builder(BroadcasterUserId: "999");

        var result = await builder.RequireBroadcasterUserId().ValidateAsync();

        Assert.True(result.HasSilentFailure);
        Assert.True(result.IsInvalid);
        Assert.Empty(result.Errors);
        Assert.Null(result.FirstError);
    }

    /// <summary>
    /// После тихой неудачи громкие проверки пропускаются: иначе зритель получил
    /// бы «это работает только на основном канале» вместо объяснения про
    /// выключенную награду, то есть не то, что его остановило.
    /// </summary>
    [Fact]
    public async Task LoudChecksAreSkippedAfterSilentFailure()
    {
        var builder = Builder(BroadcasterUserId: "999");

        var result = await builder
            .RequireBroadcasterUserId()
            .RequireServiceActive(isActive: false, loud: true)
            .ValidateAsync();

        Assert.True(result.HasSilentFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task EmptyBuilderIsValid()
    {
        var result = await Builder().ValidateAsync();

        Assert.True(result.IsValid);
        Assert.False(result.HasSilentFailure);
    }

    [Fact]
    public async Task EveryLoudErrorIsCollected()
    {
        var builder = Builder(BroadcasterUserId: "999", Cost: 1);

        var result = await builder
            .RequireBroadcasterUserId(loud: true)
            .RequireCost(100, loud: true)
            .ValidateAsync();

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("основном канале", result.FirstError);
    }

    [Fact]
    public async Task ResponseIsSentToMainChannelOnFailure()
    {
        var client = new Mock<ITwitchClient>();
        var builder = Builder(client: client.Object, BroadcasterUserId: "999");

        await builder.RequireBroadcasterUserId(loud: true).ValidateWithResponseAsync("зритель");

        client.Verify(
            instance =>
                instance.SendMessageAsync(
                    TwitchConstants.Channel,
                    It.Is<string>(message =>
                        message.Contains("@зритель") && message.Contains("основном канале")
                    ),
                    default
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task NoMessageIsSentWhenValidationPassed()
    {
        var client = new Mock<ITwitchClient>();
        var builder = Builder(client: client.Object);

        await builder.RequireCost(100).ValidateWithResponseAsync("зритель");

        client.Verify(
            instance => instance.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never
        );
    }

    /// <summary>
    /// Упавшая отправка не должна превращать успешную проверку в исключение:
    /// причина отказа уже сформулирована, зрителю её не показать — неприятно,
    /// но ломать обработчик награды из-за чата нельзя.
    /// </summary>
    [Fact]
    public async Task FailedResponseDoesNotThrow()
    {
        var client = new Mock<ITwitchClient>();
        client
            .Setup(instance =>
                instance.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), default)
            )
            .ThrowsAsync(new InvalidOperationException("чат недоступен"));
        var builder = Builder(client: client.Object, BroadcasterUserId: "999");

        var result = await builder
            .RequireBroadcasterUserId(loud: true)
            .ValidateWithResponseAsync("зритель");

        Assert.True(result.IsInvalid);
        Assert.Contains("основном канале", result.FirstError);
    }

    [Fact]
    public async Task FluentCallsReturnSameBuilder()
    {
        var builder = Builder();

        Assert.Same(builder, builder.RequireBroadcasterUserId());
        Assert.Same(builder, builder.RequireBroadcasterUserLogin());
        Assert.Same(builder, builder.RequireCost(1));
        Assert.Same(builder, builder.RequireServiceActive(true));
        Assert.Same(builder, builder.RequireRewardEnabled(() => true));
        Assert.Same(builder, builder.RequireRewardGuid(RewardId));
    }

    private static readonly Guid RewardId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static RedemptionValidationBuilder Builder(
        string? BroadcasterUserId = null,
        string? BroadcasterUserLogin = null,
        int Cost = 100,
        string? RewardIdText = null,
        ITwitchClient? client = null
    ) =>
        new(
            Args(
                BroadcasterUserId,
                BroadcasterUserLogin,
                Cost,
                RewardIdText ?? RewardId.ToString()
            ),
            client ?? Mock.Of<ITwitchClient>(),
            NullLogger.Instance
        );

    /// <summary>
    /// Событие наград TwitchLib собирается присваиванием свойств: у типов есть
    /// конструктор без параметров, а набор полей большой и меняется вместе с
    /// библиотекой.
    /// </summary>
    private static ChannelPointsCustomRewardRedemptionArgs Args(
        string? broadcasterUserId,
        string? broadcasterUserLogin,
        int cost,
        string rewardId
    ) =>
        new()
        {
            Payload = new EventSubNotificationPayload<ChannelPointsCustomRewardRedemption>
            {
                Event = new ChannelPointsCustomRewardRedemption
                {
                    BroadcasterUserId = broadcasterUserId ?? "123",
                    BroadcasterUserLogin = broadcasterUserLogin ?? "канал",
                    Reward = new RedemptionReward { Cost = cost, Id = rewardId },
                },
            },
        };
}
