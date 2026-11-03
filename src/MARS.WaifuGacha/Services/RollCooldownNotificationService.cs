using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.WaifuGacha.Configuration;
using MARS.WaifuGacha.Entities;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Уведомления «кулдаун на ролл прошёл» в чат.
/// </summary>
/// <remarks>
/// <para>
/// Раньше сервис поднимал собственные IRC + EventSub подключения теми же учётными
/// данными, что и MARS.TwitchCore. Twitch закрывает более старое из двух соединений,
/// поэтому один из сервисов молча отваливался (блокер №19 из аудита PR).
/// </para>
/// <para>
/// Теперь источники событий — общая шина: <c>twitch.reward.redeemed</c> и конкретные
/// <c>twitch.reward.&lt;name&gt;</c> (их публикует MARS.TwitchCore), а ответ в чат
/// уходит событием <c>twitch.chat.send</c>. Дополнительно на трате награды реально
/// продлевается антиспам-кулдаун в <c>waifu.RollCooldowns</c>
/// (<see cref="RollCooldownService"/>), который раньше вообще никто не вызывал.
/// Длина кулдауна берётся из <c>waifu.RootState</c> через
/// <see cref="RollCooldownConfigurationService"/> — раньше здесь был запрос с
/// префиксом <c>RootState_</c>, который не совпадал ни с одним ключом.
/// </para>
/// </remarks>
public class RollCooldownNotificationService(
    IOptions<RabbitMqOptions> options,
    IMarsEventBus eventBus,
    RollCooldownService cooldownService,
    RollCooldownConfigurationService cooldownConfiguration,
    ILogger<RollCooldownNotificationService> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "waifu",
        QueueName,
        [RabbitMqConfig.RewardEventsPattern, RabbitMqConfig.MessageReceived],
        logger
    )
{
    public const string QueueName = "waifu.rolls";

    private const int RollCost = 4;

    private const string RollTypeWaifu = RootStateKeys.WaifuRollType;
    private const string RollTypeFumo = RootStateKeys.FumoRollType;
    private const string RollTypeMiku = RootStateKeys.MikuRollType;
    private const string RollTypeFrog = RootStateKeys.FrogRollType;

    private static readonly Dictionary<string, string> RollTypeNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [RollTypeWaifu] = "WaifuRoll",
        [RollTypeMiku] = "MikuRoll",
        [RollTypeFumo] = "FumoRoll",
        [RollTypeFrog] = "FrogRoll",
    };

    private readonly Dictionary<(string UserId, string RollType), DateTime> _pendingNotifications =
        new();
    private readonly HashSet<(string UserId, string RollType)> _notifiedUsers = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        if (routingKey.StartsWith(RabbitMqConfig.RewardPrefix, StringComparison.Ordinal))
        {
            await OnRewardRedeemedAsync(json, ct);
        }
        else if (routingKey == RabbitMqConfig.MessageReceived)
        {
            await OnChatMessageAsync(json, ct);
        }
        else
        {
            logger.LogDebug(
                "Routing key {RoutingKey} ignored by roll cooldown consumer",
                routingKey
            );
        }
    }

    private async Task OnRewardRedeemedAsync(string json, CancellationToken ct)
    {
        var rewardEvent = Deserialize<RewardRedeemedEvent>(json);

        if (rewardEvent is null || string.IsNullOrWhiteSpace(rewardEvent.UserId))
        {
            return;
        }

        if (rewardEvent.Cost < RollCost)
        {
            return;
        }

        var rollType = ResolveRollType(rewardEvent.RewardTitle);

        if (rollType is null)
        {
            return;
        }

        var cooldown = await cooldownConfiguration.GetCooldownAsync(rollType, ct);

        var (allowed, remaining) = await cooldownService.CheckAndUpdateCooldownAsync(
            rewardEvent.UserId,
            rollType,
            cooldown,
            ct
        );

        if (!allowed)
        {
            logger.LogInformation(
                "Ролл {RollType} для {UserId} отклонён: кулдаун активен ещё {Remaining}",
                rollType,
                rewardEvent.UserId,
                remaining
            );
            return;
        }

        var key = (rewardEvent.UserId, rollType);
        var cooldownEnd = DateTime.UtcNow + cooldown;

        await _semaphore.WaitAsync(ct);

        try
        {
            _pendingNotifications[key] = cooldownEnd;
            _notifiedUsers.Remove(key);

            logger.LogDebug(
                "Добавлен в ожидание уведомления: {UserId} ({RollType}), кулдаун закончится: {CooldownEnd}",
                rewardEvent.UserId,
                rollType,
                cooldownEnd
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task OnChatMessageAsync(string json, CancellationToken ct)
    {
        var chatMessage = Deserialize<ChatMessageEvent>(json);

        if (chatMessage is null || string.IsNullOrWhiteSpace(chatMessage.UserId))
        {
            return;
        }

        List<(string UserId, string RollType)> expired = [];

        await _semaphore.WaitAsync(ct);

        try
        {
            var now = DateTime.UtcNow;

            var userKeys = _pendingNotifications
                .Keys.Where(key =>
                    string.Equals(key.UserId, chatMessage.UserId, StringComparison.Ordinal)
                )
                .ToList();

            foreach (var key in userKeys)
            {
                var cooldownEnd = _pendingNotifications[key];

                if (now >= cooldownEnd)
                {
                    _pendingNotifications.Remove(key);
                    expired.Add(key);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        foreach (var key in expired)
        {
            await NotifyAsync(key.UserId, key.RollType, chatMessage.UserName, ct);
        }
    }

    private async Task NotifyAsync(
        string userId,
        string rollType,
        string userName,
        CancellationToken ct
    )
    {
        var alreadyNotified = false;

        await _semaphore.WaitAsync(ct);

        try
        {
            alreadyNotified = !_notifiedUsers.Add((userId, rollType));

            if (_notifiedUsers.Count > 1000)
            {
                _notifiedUsers.Clear();
            }
        }
        finally
        {
            _semaphore.Release();
        }

        if (alreadyNotified)
        {
            return;
        }

        var rollName = RollTypeNames.GetValueOrDefault(rollType, rollType);
        var message = $"@{userName}, кулдаун на {rollName} прошел! Можешь использовать снова!";

        await eventBus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new ChatSendEvent
            {
                Channel = TwitchConstants.Channel,
                Message = message,
                ReplyTo = userId,
            },
            ct
        );

        logger.LogInformation(
            "Отправлено уведомление о завершении кулдауна {RollType} для {UserName} ({UserId})",
            rollType,
            userName,
            userId
        );
    }

    /// <summary>
    /// Определяет тип ролла по названию награды. Названия, не относящиеся к роллам,
    /// возвращают <c>null</c> — такие награды кулдауном не защищаются.
    /// </summary>
    private static string? ResolveRollType(string rewardTitle)
    {
        if (string.IsNullOrWhiteSpace(rewardTitle))
        {
            return null;
        }

        var title = rewardTitle.Trim().ToLowerInvariant();

        if (title.Contains("fumo", StringComparison.Ordinal))
        {
            return RollTypeFumo;
        }

        if (title.Contains("miku", StringComparison.Ordinal))
        {
            return RollTypeMiku;
        }

        if (
            title.Contains("frog", StringComparison.Ordinal)
            || title.Contains("жаба", StringComparison.Ordinal)
        )
        {
            return RollTypeFrog;
        }

        if (
            title.Contains("waifu", StringComparison.Ordinal)
            || title.Contains("вайфу", StringComparison.Ordinal)
        )
        {
            return RollTypeWaifu;
        }

        return null;
    }
}
