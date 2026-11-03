using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MARS.Alerts.Extensions;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Выполняет бизнес-логику Channel Points поверх исходного EventSub JSON.
/// Не является RabbitMQ consumer и не владеет отдельным транспортным маршрутом.
/// </summary>
public sealed class RewardRedemptionProcessor(
    IEnumerable<IRewardActivationEffect> effects,
    IRewardMediaResolver mediaResolver,
    RickRollerService rickRoller,
    ILogger<RewardRedemptionProcessor> logger
) : IRewardRedemptionProcessor
{
    private const int MaxCachedRedemptions = 4096;

    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyDictionary<string, IRewardActivationEffect> _effects = BuildEffectMap(
        effects,
        logger
    );

    private readonly ConcurrentDictionary<string, Lazy<Task<CacheResult>>> _processed = new(
        StringComparer.Ordinal
    );

    private readonly ConcurrentQueue<string> _cacheOrder = new();

    /// <summary>
    /// Награды, для которых исторически применялся рикролл вместо их эффекта.
    /// Это дополнительное поведение известных наград, а не требование к обычным
    /// Channel Points redemptions.
    /// </summary>
    public static readonly IReadOnlySet<string> RickRollableKeys = new HashSet<string>(
        StringComparer.Ordinal
    )
    {
        RewardAlertKeys.Confetti,
        RewardAlertKeys.Fireworks,
        RewardAlertKeys.PhonkEdit,
        RewardAlertKeys.TikTokEdit,
        RewardAlertKeys.RandomArt,
        RewardAlertKeys.MikuMikuBeam,
        RewardAlertKeys.GaoAlert,
        RewardAlertKeys.MichaelTime,
        RewardAlertKeys.FumoFridayNight,
        RewardAlertKeys.MikuMonday,
        RewardAlertKeys.AdhdSuperpower,
    };

    public async Task<string?> ProcessAsync(
        string rawEventJson,
        string? alertKey,
        Guid? mediaInfoId,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawEventJson);
        cancellationToken.ThrowIfCancellationRequested();

        var messageId = ReadEventSubMessageId(rawEventJson);

        if (string.IsNullOrWhiteSpace(messageId))
        {
            return await ProcessCoreAsync(rawEventJson, alertKey, mediaInfoId, cancellationToken);
        }

        async Task<CacheResult> CreateCacheResultAsync()
        {
            var json = await ProcessCoreAsync(
                rawEventJson,
                alertKey,
                mediaInfoId,
                cancellationToken
            );
            return new CacheResult(json);
        }

        var candidate = new Lazy<Task<CacheResult>>(
            CreateCacheResultAsync,
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        var cached = _processed.GetOrAdd(messageId, candidate);

        if (ReferenceEquals(candidate, cached))
        {
            _cacheOrder.Enqueue(messageId);
            TrimCache();
        }

        try
        {
            return (await cached.Value).Json;
        }
        catch
        {
            if (
                _processed.TryGetValue(messageId, out var current)
                && ReferenceEquals(current, cached)
            )
            {
                _processed.TryRemove(messageId, out _);
            }

            throw;
        }
    }

    /// <summary>
    /// Повторная доставка одного EventSub message ID не запускает повторно
    /// бизнес-эффект и возвращает тот же JSON для повторной отправки в SignalR.
    /// </summary>
    private void TrimCache()
    {
        while (_processed.Count > MaxCachedRedemptions && _cacheOrder.TryDequeue(out var oldest))
        {
            _processed.TryRemove(oldest, out _);
        }
    }

    private async Task<string?> ProcessCoreAsync(
        string rawEventJson,
        string? alertKey,
        Guid? mediaInfoId,
        CancellationToken cancellationToken
    )
    {
        using var document = JsonDocument.Parse(rawEventJson);
        var root = document.RootElement;

        if (
            !TryGetProperty(root, out var payloadElement, "payload")
            || !TryGetProperty(payloadElement, out var eventElement, "event")
            || eventElement.ValueKind != JsonValueKind.Object
        )
        {
            throw new InvalidOperationException(
                "Channel Points EventSub notification does not contain payload.event"
            );
        }

        if (
            !TryGetProperty(eventElement, out var rewardElement, "reward")
            || rewardElement.ValueKind != JsonValueKind.Object
        )
        {
            throw new InvalidOperationException(
                "Channel Points EventSub notification does not contain payload.event.reward"
            );
        }

        var rewardId = ReadString(rewardElement, "id");
        if (string.IsNullOrWhiteSpace(rewardId))
        {
            throw new InvalidOperationException(
                "Channel Points EventSub notification does not contain reward.id"
            );
        }

        var userId = ReadString(eventElement, "user_id") ?? string.Empty;
        var userLogin = ReadString(eventElement, "user_login");
        var userName =
            ReadString(eventElement, "user_name")
            ?? userLogin
            ?? (string.IsNullOrWhiteSpace(userId) ? "Зритель" : userId);

        var messageId = ReadString(eventElement, "id");
        var eventSubMessageId = ReadString(root, "metadata", "message_id");

        var reward = new RewardRedeemedEvent
        {
            RewardId = rewardId,
            RewardTitle = ReadString(rewardElement, "title") ?? "Новая награда",
            AlertKey = alertKey,
            MediaInfoId = mediaInfoId,
            Cost = ReadInt32(rewardElement, "cost"),
            UserId = userId,
            UserName = userName,
            UserInput = ReadString(eventElement, "user_input"),
            RedeemedAt = ReadDateTime(eventElement, "redeemed_at") ?? DateTime.UtcNow,
            MessageId = messageId,
            EventSubMessageId = eventSubMessageId,
            User = string.IsNullOrWhiteSpace(userId)
                ? null
                : new TwitchUser
                {
                    TwitchId = userId,
                    UserLogin = userLogin ?? userName,
                    DisplayName = userName,
                },
        };

        var normalizedAlertKey = alertKey?.Trim();

        if (
            !string.IsNullOrWhiteSpace(normalizedAlertKey)
            && reward.User is not null
            && RickRollableKeys.Contains(normalizedAlertKey)
            && await rickRoller.TryRickRollAsync(reward.User, () => Task.CompletedTask)
        )
        {
            logger.LogInformation(
                "Reward {RewardId} by {UserName} replaced with rickroll",
                rewardId,
                userName
            );
            return null;
        }

        IReadOnlyList<MediaInfo> resolvedMedia;

        try
        {
            resolvedMedia = await mediaResolver.ResolveAsync(reward, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Could not resolve media for reward {RewardId}; generic activation will continue",
                rewardId
            );
            resolvedMedia = [];
        }

        var effectResult = RewardEffectResult.Passthrough;

        if (
            !string.IsNullOrWhiteSpace(normalizedAlertKey)
            && _effects.TryGetValue(normalizedAlertKey, out var effect)
        )
        {
            effectResult = await effect.ApplyAsync(
                new RewardActivationContext { Reward = reward, Media = resolvedMedia },
                cancellationToken
            );
        }

        if (effectResult.Suppress)
        {
            logger.LogInformation(
                "Reward {RewardId} ({AlertKey}) intentionally suppressed its generic overlay",
                rewardId,
                normalizedAlertKey
            );
            return null;
        }

        var user = ResolveUser(reward);
        var media = (effectResult.Media ?? resolvedMedia)
            .Select(info => PrepareMedia(info, user, reward.UserInput))
            .ToArray();

        var eventId = eventSubMessageId ?? messageId;
        var output =
            JsonNode.Parse(rawEventJson) as JsonObject
            ?? throw new InvalidOperationException(
                "EventSub notification root must be a JSON object"
            );

        var mars = output["mars"] as JsonObject;
        if (mars is null)
        {
            mars = new JsonObject();
            output["mars"] = mars;
        }

        mars["reward"] = JsonSerializer.SerializeToNode(
            new
            {
                type = "mars.reward.activated",
                eventId,
                payload = new
                {
                    rewardId = reward.RewardId,
                    rewardTitle = reward.RewardTitle,
                    alertKey = normalizedAlertKey,
                    user,
                    userInput = reward.UserInput,
                    cost = reward.Cost,
                    redeemedAt = reward.RedeemedAt,
                    media,
                    effectData = effectResult.Data,
                },
            },
            WebJsonOptions
        );

        logger.LogInformation(
            "Reward {RewardId} ({AlertKey}) activated by {UserName} for {Cost} points",
            reward.RewardId,
            normalizedAlertKey,
            reward.UserName,
            reward.Cost
        );

        // metadata и payload Twitch остаются доступны без изменения. MARS
        // добавляет только своё расширение в отдельное top-level пространство.
        return output.ToJsonString(WebJsonOptions);
    }

    public static IReadOnlyDictionary<string, IRewardActivationEffect> BuildEffectMap(
        IEnumerable<IRewardActivationEffect> effects,
        ILogger logger
    )
    {
        var registered = new Dictionary<string, IRewardActivationEffect>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var effect in effects)
        {
            if (!registered.TryAdd(effect.AlertKey, effect))
            {
                duplicates.Add(effect.AlertKey);
            }
        }

        if (duplicates.Count > 0)
        {
            logger.LogWarning(
                "Duplicate reward activation effects registered for keys: {Keys}. First registration wins.",
                string.Join(", ", duplicates)
            );
        }

        return registered;
    }

    private static TwitchUser ResolveUser(RewardRedeemedEvent reward)
    {
        return reward.User
            ?? new TwitchUser
            {
                TwitchId = reward.UserId,
                UserLogin = reward.UserName,
                DisplayName = reward.UserName,
            };
    }

    private static MediaInfo PrepareMedia(MediaInfo media, TwitchUser user, string? userInput)
    {
        var clone = media.CloneTo();
        clone.FixAlertText(user, userInput ?? string.Empty);
        clone.FixAlertColor(user);
        return clone;
    }

    private static string? ReadEventSubMessageId(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ReadString(document.RootElement, "metadata", "message_id")
            ?? ReadString(document.RootElement, "payload", "event", "id");
    }

    private static string? ReadString(JsonElement root, params string[] path)
    {
        var current = root;

        foreach (var segment in path)
        {
            if (!TryGetProperty(current, out var next, segment))
            {
                return null;
            }

            current = next;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static int ReadInt32(JsonElement root, string name)
    {
        return
            TryGetProperty(root, out var value, name)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var result)
            ? result
            : 0;
    }

    private static DateTime? ReadDateTime(JsonElement root, string name)
    {
        var value = ReadString(root, name);

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result
        )
            ? result
            : null;
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement value, string name)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private sealed record CacheResult(string? Json);
}
