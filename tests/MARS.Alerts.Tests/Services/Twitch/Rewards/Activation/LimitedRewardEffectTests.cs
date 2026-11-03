using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда с лимитами (Fumo Friday Night, Miku Monday Alert).
/// </summary>
/// <remarks>
/// Повтор зрителя возвращает баллы, общий кулдаун — нет: так было в монолите,
/// и это единственное отличие двух отказов, видимое зрителю. Тексты в чате
/// проверяются целиком по смыслу: именно по ним зритель понимает, вернули ему
/// баллы или нет.
/// </remarks>
public class LimitedRewardEffectTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static readonly LimitedRewardRule Rule = new(
        "mikumonday",
        "Miku Monday Alert",
        "Miku Monday Alert! 🎤",
        TimeSpan.FromMinutes(20)
    );

    private readonly Mock<IRewardChatNotifier> _chat = new();
    private readonly Mock<IRewardRefundRequestor> _refund = new();
    private readonly List<string> _messages = [];
    private readonly RewardTestTimeProvider _time = new(Start);

    public LimitedRewardEffectTests()
    {
        _chat
            .Setup(chat =>
                chat.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string?, CancellationToken>((message, _, _) => _messages.Add(message))
            .Returns(Task.CompletedTask);

        _refund
            .Setup(refund =>
                refund.RequestAsync(It.IsAny<RewardRefundEvent>(), It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void EffectServesItsOwnKey()
    {
        Assert.Equal(Rule.AlertKey, CreateEffect().AlertKey);
    }

    [Fact]
    public async Task FirstActivationIsShownAndAnnounced()
    {
        var result = await CreateEffect()
            .ApplyAsync(Context("user-1", "Pyro"), TestContext.Current.CancellationToken);

        Assert.False(result.Suppress);
        var data = JsonSerializer.SerializeToElement(result.Data);
        Assert.Equal("Pyro", data.GetProperty("DisplayName").GetString());
        Assert.Contains("активировал Miku Monday Alert! 🎤", Assert.Single(_messages));
        _refund.Verify(
            refund =>
                refund.RequestAsync(It.IsAny<RewardRefundEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    /// <summary>
    /// Повтор зрителем возвращает баллы: он уже видел награду в этом стриме,
    /// и платить второй раз ему не за что.
    /// </summary>
    [Fact]
    public async Task RepeatedActivationReturnsPoints()
    {
        var effect = CreateEffect();

        await effect.ApplyAsync(Context("user-1", "Pyro"), TestContext.Current.CancellationToken);
        var result = await effect.ApplyAsync(
            Context("user-1", "Pyro"),
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Suppress);
        _refund.Verify(
            refund =>
                refund.RequestAsync(
                    It.Is<RewardRefundEvent>(request =>
                        request.RewardId == "reward-1"
                        && request.RedemptionId == "redemption-1"
                        && request.UserName == "Pyro"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        Assert.Contains("Баллы возвращены", _messages[^1]);
    }

    /// <summary>
    /// Общий кулдаун баллы не возвращает: зритель сам нажал награду, которая
    /// сейчас недоступна. Он получает только сообщение со сроком ожидания.
    /// </summary>
    [Fact]
    public async Task GlobalCooldownOnlyReportsRemainingSeconds()
    {
        var effect = CreateEffect();

        await effect.ApplyAsync(Context("user-1", "Pyro"), TestContext.Current.CancellationToken);
        var result = await effect.ApplyAsync(
            Context("user-2", "Second"),
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Suppress);
        _refund.Verify(
            refund =>
                refund.RequestAsync(It.IsAny<RewardRefundEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        Assert.Contains("общий кулдаун Miku Monday Alert", _messages[^1]);
        Assert.Contains("Осталось 1200 сек.", _messages[^1]);
    }

    [Fact]
    public async Task AnotherUserIsAllowedAfterCooldown()
    {
        var effect = CreateEffect();

        await effect.ApplyAsync(Context("user-1", "Pyro"), TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromMinutes(20));
        var result = await effect.ApplyAsync(
            Context("user-2", "Second"),
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Suppress);
    }

    [Fact]
    public async Task LimitsResetOnNextDay()
    {
        var effect = CreateEffect();

        await effect.ApplyAsync(Context("user-1", "Pyro"), TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromDays(1));
        var result = await effect.ApplyAsync(
            Context("user-1", "Pyro"),
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Suppress);
        _refund.Verify(
            refund =>
                refund.RequestAsync(It.IsAny<RewardRefundEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private LimitedRewardEffect CreateEffect() =>
        new(
            Rule,
            new PerStreamRewardLimiter(_time),
            _chat.Object,
            _refund.Object,
            NullLogger<LimitedRewardEffect>.Instance
        );

    private static RewardActivationContext Context(string userId, string userName) =>
        new()
        {
            Reward = new RewardRedeemedEvent
            {
                RewardId = "reward-1",
                RewardTitle = "Miku Monday",
                AlertKey = Rule.AlertKey,
                UserId = userId,
                UserName = userName,
                Cost = 1000,
                MessageId = "redemption-1",
                User = new TwitchUser
                {
                    TwitchId = userId,
                    UserLogin = userName.ToLowerInvariant(),
                    DisplayName = userName,
                    ChatColor = "#FF0000",
                },
            },
            Media = [],
        };
}
