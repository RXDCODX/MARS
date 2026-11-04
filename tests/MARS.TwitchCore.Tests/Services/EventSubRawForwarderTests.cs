using System.Reflection;
using System.Text;
using System.Text.Json;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.EventSub;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Сырой EventSub-фрейм уходит в оверлей как есть, а редемпшн награды — ещё и
/// отдельным событием тем, кому нужен только факт траты.
/// </summary>
/// <remarks>
/// <para>
/// Разделение намеренное: обработчику оверлея нужен весь исходный JSON Twitch,
/// а <c>MARS.WaifuGacha</c> — только пользователь, стоимость и ключ алерта.
/// Пока эти два потока были одним, потребитель без разбора raw не получал
/// ничего, и кулдаун ролла не продлевался.
/// </para>
/// <para>
/// Проверяется и обратное: обычное событие (например, фоловер) активацией не
/// считается. Иначе каждое уведомление EventSub выглядело бы как трата баллов.
/// </para>
/// </remarks>
public class EventSubRawForwarderTests : IAsyncLifetime
{
    private const string RewardId = "reward-1";
    private const string RollAlertKey = "fumo-roll";

    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly List<(string Key, object Message)> _published = [];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task RedemptionIsForwardedAsRawAndAsActivation()
    {
        await SeedRewardAsync(
            RollAlertKey,
            mediaInfoId: Guid.Parse("8c4b0f2e-1d3a-4b5c-9e8f-7a6b5c4d3e2f")
        );

        await PublishAsync(RedemptionFrame());

        Assert.Equal(
            [
                RabbitMqConfig.RoutingKeys.EventSubReceived,
                RabbitMqConfig.RoutingKeys.RewardActivated,
            ],
            _published.Select(item => item.Key).ToArray()
        );

        var activation = Assert.IsType<RewardRedeemedEvent>(_published[1].Message);
        Assert.Equal(RewardId, activation.RewardId);
        Assert.Equal("🎲 Фумо-ролл", activation.RewardTitle);
        Assert.Equal(RollAlertKey, activation.AlertKey);
        Assert.Equal(100, activation.Cost);
        Assert.Equal("42", activation.UserId);
        Assert.Equal("Viewer", activation.UserName);
        Assert.Equal("хочу фумо", activation.UserInput);
        Assert.Equal("redemption-1", activation.MessageId);
        Assert.Equal("event-message-1", activation.EventSubMessageId);
        Assert.NotNull(activation.MediaInfoId);
        Assert.Equal(DateTimeKind.Utc, activation.RedeemedAt.Kind);
    }

    [Fact]
    public async Task RedemptionWithoutLocalCatalogEntryIsStillActivated()
    {
        // Награда может существовать только в Twitch: каталог MARS её не знает.
        // Трата всё равно произошла, и событие обязано уйти.
        await PublishAsync(RedemptionFrame());

        Assert.Equal(
            [
                RabbitMqConfig.RoutingKeys.EventSubReceived,
                RabbitMqConfig.RoutingKeys.RewardActivated,
            ],
            _published.Select(item => item.Key).ToArray()
        );

        var activation = Assert.IsType<RewardRedeemedEvent>(_published[1].Message);
        Assert.Equal(RewardId, activation.RewardId);
        Assert.Null(activation.AlertKey);
        Assert.Null(activation.MediaInfoId);
    }

    [Fact]
    public async Task NonRewardNotificationIsOnlyForwardedAsRaw()
    {
        await PublishAsync(Frame("channel.follow", new { user_id = "7", user_name = "Follower" }));

        Assert.Equal(
            [RabbitMqConfig.RoutingKeys.EventSubReceived],
            _published.Select(item => item.Key).ToArray()
        );
    }

    private async Task SeedRewardAsync(string alertKey, Guid mediaInfoId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        db.ChannelRewards.Add(
            new ChannelRewardRecord
            {
                Title = "🎲 Фумо-ролл",
                Cost = 100,
                TwitchRewardId = RewardId,
                AlertKey = alertKey,
                MediaInfoId = mediaInfoId,
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task PublishAsync(byte[] frame)
    {
        var forwarder = new EventSubRawForwarder(
            OfflineEventSub.Create(),
            new RecordingEventBus(_published),
            _factory,
            NullLogger<EventSubRawForwarder>.Instance
        );

        var method = typeof(EventSubRawForwarder).GetMethod(
            "PublishRawFrameAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(forwarder, [frame, TestContext.Current.CancellationToken])!;
    }

    private static byte[] RedemptionFrame()
    {
        return Frame(
            "channel.channel_points_custom_reward_redemption.add",
            new
            {
                id = "redemption-1",
                broadcaster_user_id = "1",
                broadcaster_user_login = "streamer",
                user_id = "42",
                user_login = "viewer",
                user_name = "Viewer",
                user_input = "хочу фумо",
                status = "unfulfilled",
                redeemed_at = "2026-11-03T10:00:00.000Z",
                reward = new
                {
                    id = RewardId,
                    title = "🎲 Фумо-ролл",
                    cost = 100,
                    prompt = string.Empty,
                },
            }
        );
    }

    private static byte[] Frame(string subscriptionType, object eventPayload)
    {
        return Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(
                new
                {
                    metadata = new
                    {
                        message_id = "event-message-1",
                        message_type = "notification",
                        subscription_type = subscriptionType,
                        subscription_version = "1",
                    },
                    payload = new
                    {
                        subscription = new { id = "subscription-1", type = subscriptionType },
                        @event = eventPayload,
                    },
                }
            )
        );
    }

    private sealed class RecordingEventBus(List<(string Key, object Message)> published)
        : IMarsEventBus
    {
        public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
        {
            published.Add((routingKey, message!));
            return Task.CompletedTask;
        }
    }
}
