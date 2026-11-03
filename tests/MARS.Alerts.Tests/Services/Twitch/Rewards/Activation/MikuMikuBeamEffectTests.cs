using System.Text.Json;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда MIKU MIKU BEAM: участники окна чата и пауза между лучами.
/// </summary>
/// <remarks>
/// Пауза не возвращает баллы и не является лимитом награды: она лишь не даёт
/// перезапустить луч, пока предыдущий ещё идёт. Проверяется именно это, а не
/// текст сообщения: сообщений у эффекта нет, он возвращает данные.
/// </remarks>
public class MikuMikuBeamEffectTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly RewardTestTimeProvider _time = new(Start);

    [Fact]
    public void EffectServesItsOwnKey()
    {
        Assert.Equal(RewardAlertKeys.MikuMikuBeam, CreateEffect().AlertKey);
    }

    [Fact]
    public async Task BeamContainsTrackedUsers()
    {
        var effect = CreateEffect();
        effect.TrackChatUser(Message("u1"));
        effect.TrackChatUser(Message("u2"));

        var result = await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);

        Assert.False(result.Suppress);
        Assert.Equal(["u1", "u2"], Users(result));
    }

    [Fact]
    public async Task RepeatedChatUserIsStoredOnce()
    {
        var effect = CreateEffect();
        effect.TrackChatUser(Message("u1"));
        effect.TrackChatUser(Message("u1"));
        effect.TrackChatUser(Message("u2"));

        var result = await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);

        Assert.Equal(["u1", "u2"], Users(result));
    }

    /// <summary>
    /// При переполнении вытесняется самый старый участник, а не только что
    /// добавленный: иначе луч всегда состоял бы из одного последнего зрителя.
    /// </summary>
    [Fact]
    public async Task OldestUserIsEvictedFromWindow()
    {
        var effect = CreateEffect();

        for (var index = 0; index <= 100; index++)
        {
            effect.TrackChatUser(Message($"u{index}"));
        }

        var result = await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);

        var users = Users(result);
        Assert.Equal(100, users.Length);
        Assert.DoesNotContain("u0", users);
        Assert.Contains("u100", users);
    }

    [Fact]
    public async Task ImmediateReactivationIsSuppressed()
    {
        var effect = CreateEffect();

        var first = await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);
        var second = await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);

        Assert.False(first.Suppress);
        Assert.True(second.Suppress);
    }

    [Fact]
    public async Task BeamRestartsAfterCooldown()
    {
        var effect = CreateEffect();

        await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromSeconds(60));
        var second = await effect.ApplyAsync(Context(), TestContext.Current.CancellationToken);

        Assert.False(second.Suppress);
    }

    private MikuMikuBeamEffect CreateEffect() =>
        new(_time, NullLogger<MikuMikuBeamEffect>.Instance);

    private static string[] Users(RewardEffectResult result) =>
        JsonSerializer
            .SerializeToElement(result.Data)
            .GetProperty("Users")
            .EnumerateArray()
            .Select(user => user.GetProperty("TwitchId").GetString()!)
            .ToArray();

    private static ChatMessageEvent Message(string userId) =>
        new()
        {
            UserId = userId,
            UserName = userId,
            Message = "hello",
        };

    private static RewardActivationContext Context() =>
        new()
        {
            Reward = new RewardRedeemedEvent
            {
                AlertKey = RewardAlertKeys.MikuMikuBeam,
                UserId = "42",
                UserName = "Pyro",
                Cost = 100,
            },
            Media = [],
        };
}
