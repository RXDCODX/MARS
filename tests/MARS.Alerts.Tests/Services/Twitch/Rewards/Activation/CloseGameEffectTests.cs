using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Выключить игру»: закрывает процессы и не показывает ничего.
/// </summary>
/// <remarks>
/// Процессы подменены интерфейсом: убивать игру на машине разработчика тест не
/// должен, иначе он проверял бы наличие игры, а не логику награды.
/// </remarks>
public class CloseGameEffectTests
{
    private readonly Mock<IGameCloser> _closer = new();

    [Fact]
    public void EffectServesItsOwnKey()
    {
        Assert.Equal(RewardAlertKeys.CloseGame, CreateEffect().AlertKey);
    }

    [Fact]
    public async Task GameIsClosedAndNothingIsShown()
    {
        _closer
            .Setup(closer => closer.CloseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateEffect()
            .ApplyAsync(Context(), TestContext.Current.CancellationToken);

        Assert.True(result.Suppress);
        _closer.Verify(closer => closer.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CloseGameEffect CreateEffect() =>
        new(_closer.Object, NullLogger<CloseGameEffect>.Instance);

    private static RewardActivationContext Context() =>
        new()
        {
            Reward = new RewardRedeemedEvent
            {
                AlertKey = RewardAlertKeys.CloseGame,
                UserId = "42",
                UserName = "Pyro",
                Cost = 100,
            },
            Media = [],
        };
}
