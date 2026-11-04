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
    /// Legacy routing key'ы конкретных наград, которые использовались до перехода на
    /// общий <see cref="RewardRedeemed"/>. Оставлены только для обратной совместимости
    /// на переходный период: <see cref="RewardAlertConsumer"/> биндится на них, чтобы
    /// не потерять события от publisher'ов, которые ещё не перешли на общий ключ.
    /// Публиковать под эти ключи больше не следует — все погашения уходят через
    /// <see cref="RewardRedeemed"/>, а выбор бизнес-обработчика делается внутри
    /// <see cref="RewardAlertConsumer"/> по нормализованному названию награды.
    /// После завершения перехода массив можно очистить и убрать лишние биндинги.
    /// </summary>
    public static readonly string[] LegacyRewardKeys =
    [
        "twitch.reward.confetti",
        "twitch.reward.fireworks",
        "twitch.reward.phonkedit",
        "twitch.reward.tiktokedit",
        "twitch.reward.randomart",
        "twitch.reward.mikumikubeam",
        "twitch.reward.gaoalert",
        "twitch.reward.what",
        "twitch.reward.pedro",
        "twitch.reward.tyazhelo",
        "twitch.reward.michaeltime",
        "twitch.reward.legbum",
        "twitch.reward.fumofridaynight",
        "twitch.reward.stone",
        "twitch.reward.cringe",
        "twitch.reward.hello",
        "twitch.reward.bye",
        "twitch.reward.intelligence",
        "twitch.reward.mikuscreamer",
        "twitch.reward.skibidibop",
        "twitch.reward.aga",
        "twitch.reward.badtobone",
        "twitch.reward.dancedance",
        "twitch.reward.wednsdayfrog",
        "twitch.reward.edge0100alert",
        "twitch.reward.skibidiboplong",
        "twitch.reward.statusquestion",
        "twitch.reward.cinemarequest",
        "twitch.reward.adhdsuperpower",
        "twitch.reward.closegame",
        "twitch.reward.selectgame",
        "twitch.reward.credits",
        "twitch.reward.allrefund",
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
