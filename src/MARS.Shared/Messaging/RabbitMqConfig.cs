using System.Text;

namespace MARS.Shared.Messaging;

public static class RabbitMqConfig
{
    public const string SectionName = "RabbitMq";
    public const string ExchangeName = "mars.events";

    // Queues
    public const string AlertsQueue = "alerts.events";
    public const string SystemEventsQueue = "alerts.system";
    public const string AdminQueue = "admin.events";

    // Routing keys
    public const string RewardRedeemed = "twitch.reward.redeemed";
    public const string RewardEventsPattern = "twitch.reward.#";
    public const string UserJoined = "twitch.user.joined";
    public const string MessageReceived = "twitch.message.received";
    public const string MessageDeleted = "twitch.message.deleted";

    /// <summary>
    /// Сообщение, отправленное через награду с вводом текста. Отдельный ключ,
    /// потому что в оверлей чата такое сообщение не попадает, но по нему
    /// поднимается привязанный к награде алерт (AC.S08): подмешивать такие
    /// сообщения в <see cref="MessageReceived"/> нельзя — потребители обычного
    /// чата ответили бы на награду как на обычное сообщение.
    /// </summary>
    public const string RewardInputMessage = "twitch.message.rewardinput";

    /// <summary>
    /// Запрос на отправку сообщения в чат. Владелец единственного IRC-соединения —
    /// MARS.TwitchCore, поэтому остальные сервисы не поднимают своё подключение
    /// (иначе Twitch отключает более старое из двух), а публикуют это событие.
    /// </summary>
    public const string ChatSend = "twitch.chat.send";

    public const string WaifuRollResult = "waifu.roll.result";
    public const string FumoRollResult = "waifu.fumo.result";
    public const string MikuRollResult = "waifu.miku.result";
    public const string FrogRollResult = "waifu.frog.result";
    public const string TrackStarted = "media.track.started";
    public const string TrackEnded = "media.track.ended";
    public const string TrackAdded = "media.track.added";

    /// <summary>
    /// Routing keys системных событий, которые читает очередь alerts.system.</summary>
    public static readonly string[] SystemRoutingKeys =
    [
        WaifuRollResult,
        FumoRollResult,
        MikuRollResult,
        FrogRollResult,
        TrackStarted,
        TrackEnded,
        TrackAdded,
    ];

    /// <summary>
    /// Нормализует название награды Twitch в идентификатор обработчика: регистр
    /// в нижний, все символы кроме букв/цифр отбрасываются. Используется
    /// <see cref="RewardAlertConsumer"/> для диспетчеризации погашений по
    /// внутреннему <see cref="IRewardAlertHandler.RewardHandlerKey"/> вместо
    /// транспортного routing key, так как все погашения теперь публикуются с
    /// одним ключом <see cref="RewardRedeemed"/>.
    /// </summary>
    public static string NormalizeRewardTitle(string rewardTitle)
    {
        var normalized = new StringBuilder(rewardTitle.Length);

        foreach (var symbol in rewardTitle)
        {
            if (char.IsLetterOrDigit(symbol))
            {
                normalized.Append(char.ToLowerInvariant(symbol));
            }
        }

        return normalized.ToString();
    }
}
