using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Вернуть всем»: экран возврата показывает зрителя, который её купил.
/// </summary>
/// <remarks>
/// Без зрителя показывать нечего: имя и цвет пришли бы пустыми, и клиент
/// нарисовал бы плашку с пустым местом вместо зрителя.
/// </remarks>
public class AllRefundEffectTests
{
    [Fact]
    public void EffectServesItsOwnKey()
    {
        Assert.Equal(RewardAlertKeys.AllRefund, new AllRefundEffect().AlertKey);
    }

    [Fact]
    public async Task ViewerIsSentAsData()
    {
        var user = User();

        var result = await new AllRefundEffect().ApplyAsync(
            Context(user),
            TestContext.Current.CancellationToken
        );

        Assert.False(result.Suppress);
        Assert.Same(user, result.Data);
    }

    [Fact]
    public async Task MissingViewerSuppressesEvent()
    {
        var result = await new AllRefundEffect().ApplyAsync(
            Context(user: null),
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Suppress);
        Assert.Null(result.Data);
    }

    private static TwitchUser User() =>
        new()
        {
            TwitchId = "42",
            UserLogin = "pyro",
            DisplayName = "Pyro",
            ChatColor = "#FF0000",
        };

    private static RewardActivationContext Context(TwitchUser? user) =>
        new()
        {
            Reward = new RewardRedeemedEvent
            {
                AlertKey = RewardAlertKeys.AllRefund,
                UserId = "42",
                UserName = "Pyro",
                Cost = 100,
                User = user,
            },
            Media = [],
        };
}
