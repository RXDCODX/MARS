using System.Text.Json;
using MARS.Alerts.Models;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда GAO Alert: ввод зрителя — ник или произвольный текст.
/// </summary>
/// <remarks>
/// Разбор на сервере, потому что от него зависит форма данных, а не
/// оформление. Ограничение в 25 символов — это ограничение Twitch на логин, и
/// строка длиннее него уже не ник.
/// </remarks>
public class GaoAlertEffectTests
{
    [Fact]
    public void EffectServesItsOwnKey()
    {
        Assert.Equal(RewardAlertKeys.GaoAlert, new GaoAlertEffect().AlertKey);
    }

    [Theory]
    [InlineData("some_user", "some_user")]
    [InlineData("@some_user", "some_user")]
    [InlineData("@SomeUser123", "SomeUser123")]
    public void SingleWordIsTreatedAsLogin(string userInput, string expected)
    {
        var data = Data(userInput);

        Assert.False(data.IsJustText);
        Assert.NotNull(data.TwitchUser);
        Assert.Equal(
            expected,
            JsonSerializer
                .SerializeToElement(data.TwitchUser)
                .GetProperty("DisplayName")
                .GetString()
        );
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("@hello world")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("user-name")]
    [InlineData("abcdefghijklmnopqrstuvwxyz")]
    public void EverythingElseIsShownAsText(string userInput)
    {
        var data = Data(userInput);

        Assert.True(data.IsJustText);
        Assert.Equal(userInput.Trim(), data.JustText);
        Assert.Null(data.TwitchUser);
    }

    [Fact]
    public void MissingInputIsEmptyText()
    {
        var data = Data(null);

        Assert.True(data.IsJustText);
        Assert.Equal(string.Empty, data.JustText);
    }

    private static GaoAlertDto Data(string? userInput)
    {
        var context = new RewardActivationContext
        {
            Reward = new RewardRedeemedEvent
            {
                AlertKey = RewardAlertKeys.GaoAlert,
                UserId = "42",
                UserName = "Pyro",
                UserInput = userInput,
                Cost = 100,
            },
            Media = [],
        };

        var result = new GaoAlertEffect()
            .ApplyAsync(context, TestContext.Current.CancellationToken)
            .GetAwaiter()
            .GetResult();

        return Assert.IsType<GaoAlertDto>(result.Data);
    }
}
