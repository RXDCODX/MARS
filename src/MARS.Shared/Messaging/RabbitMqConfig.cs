namespace MARS.Shared.Messaging;

public static class RabbitMqConfig
{
    public const string SectionName = "RabbitMq";
    public const string ExchangeName = "mars.events";

    // Queues
    public const string SystemEventsQueue = "alerts.system";
    public const string AdminQueue = "admin.events";

    /// <summary>Префикс routing key'ов наград.</summary>
    public const string RewardPrefix = "twitch.reward.";

    // Routing keys
    /// <summary>
    /// Единственный reward-ключ: что именно рисовать, решает клиент по
    /// <c>AlertKey</c> из тела события.
    /// </summary>
    public const string RewardActivated = RewardPrefix + "activated";

    /// <summary>
    /// Просьба вернуть поинты за редемпшн. Публикуют те, у кого нет токена
    /// Twitch (MARS.Alerts), исполняет владелец токена (MARS.TwitchCore).
    /// </summary>
    public const string RewardRefund = RewardPrefix + "refund";

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

    /// <summary>Routing keys системных событий, которые читает очередь alerts.system.</summary>
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
}
