using System.Text.Json;
using MARS.Alerts.Extensions;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Единственный потребитель активаций наград канала в MARS.Alerts.
/// </summary>
/// <remarks>
/// <para>
/// Очередь одна, ключ один — <see cref="RabbitMqConfig.RewardActivated"/>, а что
/// именно произошло, видно по <c>AlertKey</c> в теле. Раньше на каждую награду
/// приходил свой routing key, построенный из названия в Twitch: переименование
/// награды ломало маршрут молча.
/// </para>
/// <para>
/// Потребитель делает то, что общее для всех наград: проверяет рикролл,
/// разрешает медиа, зовёт серверный эффект (если он есть), готовит медиа к
/// показу и отправляет JSON через универсальный <c>TwitchEventSub</c> канал.
/// Отсутствие AlertKey означает обычную награду, а не пропуск события.
/// </para>
/// <para>
/// Рикролл централизован намеренно. В монолите его вызывал почти каждый
/// обработчик, и награда, добавленная без этой проверки, тихо переставала
/// роллиться. Список роллящихся ключей — часть поведения, а не деталь эффекта.
/// </para>
/// </remarks>
public sealed class RewardActivatedConsumer(
    IOptions<RabbitMqOptions> options,
    IEnumerable<IRewardActivationEffect> effects,
    IRewardMediaResolver mediaResolver,
    RickRollerService rickRoller,
    ITelegramusNotifier notifier,
    ILogger<RewardActivatedConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.RewardActivated],
        logger
    )
{
    public const string QueueName = "alerts.rewards";

    /// <summary>
    /// Награды, которые могут «рикроллиться» вместо своего эффекта. Список
    /// повторяет монолит: там рикролл стоял у этих же наград, а у остальных его
    /// не было (закрытие игры или возврат баллов роллить нельзя).
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

    private readonly IReadOnlyDictionary<string, IRewardActivationEffect> _effects = BuildEffectMap(
        effects,
        logger
    );

    /// <summary>
    /// Эффекты, зарегистрированные на один и тот же ключ. Отдельный метод —
    /// чтобы тест проверял тот же разбор, что и рабочий потребитель.
    /// </summary>
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

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var reward = Deserialize<RewardRedeemedEvent>(json);

        if (reward is null)
        {
            throw new InvalidOperationException(
                $"Reward activation on '{routingKey}' could not be deserialized into RewardRedeemedEvent"
            );
        }

        var alertKey = reward.AlertKey?.Trim();

        // Рикролл и серверный эффект — дополнительные правила известных наград.
        // Обычный redemption без AlertKey всё равно проходит в универсальный поток.
        if (!string.IsNullOrWhiteSpace(alertKey) && await ShouldRickRollAsync(reward, alertKey))
        {
            return;
        }

        var resolvedMedia = await mediaResolver.ResolveAsync(reward, ct);
        var effectResult = RewardEffectResult.Passthrough;

        if (!string.IsNullOrWhiteSpace(alertKey) && _effects.TryGetValue(alertKey, out var effect))
        {
            effectResult = await effect.ApplyAsync(
                new RewardActivationContext { Reward = reward, Media = resolvedMedia },
                ct
            );
        }

        if (effectResult.Suppress)
        {
            logger.LogInformation(
                "Reward '{AlertKey}' activated by {UserName} without overlay event",
                alertKey,
                reward.UserName
            );
            return;
        }

        var user = ResolveUser(reward);
        var media = (effectResult.Media ?? resolvedMedia)
            .Select(info => PrepareMedia(info, user, reward.UserInput))
            .ToArray();

        var eventJson = JsonSerializer.Serialize(
            new
            {
                type = "mars.reward.activated",
                eventId = reward.MessageId,
                payload = new
                {
                    rewardId = reward.RewardId,
                    rewardTitle = reward.RewardTitle,
                    alertKey,
                    user,
                    userInput = reward.UserInput,
                    cost = reward.Cost,
                    redeemedAt = reward.RedeemedAt,
                    media,
                    effectData = effectResult.Data,
                },
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );

        await notifier.TwitchEventSub(eventJson);

        logger.LogInformation(
            "Reward '{RewardId}' ({AlertKey}) activated by {UserName} for {Cost} points",
            reward.RewardId,
            alertKey,
            reward.UserName,
            reward.Cost
        );
    }

    /// <summary>
    /// Проверяет, не выпал ли рикролл. Возвращает <c>true</c>, если награда уже
    /// показана как рикролл и активацию отправлять не нужно.
    /// </summary>
    private async Task<bool> ShouldRickRollAsync(RewardRedeemedEvent reward, string alertKey)
    {
        var result = false;

        if (
            RickRollableKeys.Contains(alertKey)
            && reward.User is not null
            && await rickRoller.TryRickRollAsync(reward.User, () => Task.CompletedTask)
        )
        {
            logger.LogInformation(
                "Reward '{AlertKey}' by {UserName} replaced with rickroll",
                alertKey,
                reward.UserName
            );
            result = true;
        }

        return result;
    }

    /// <summary>
    /// Зритель из события, а при его отсутствии — собранный из полей редемпшена:
    /// у части старых сообщений <c>User</c> пуст, а клиенту нужны имя и цвет.
    /// </summary>
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

    /// <summary>
    /// Копия медиа с подставленными именем и цветом зрителя. Копия обязательна:
    /// <see cref="MediaInfoExtensions.FixAlertText"/> и
    /// <see cref="MediaInfoExtensions.FixAlertColor"/> меняют объект на месте, и
    /// без клона шаблон из хранилища остался бы с чужим именем.
    /// </summary>
    private static MediaInfo PrepareMedia(MediaInfo media, TwitchUser user, string? userInput)
    {
        var clone = media.CloneTo();

        clone.FixAlertText(user, userInput ?? string.Empty);
        clone.FixAlertColor(user);

        return clone;
    }
}
