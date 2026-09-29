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

    /// <summary>Префикс routing key'ов наград.</summary>
    public const string RewardPrefix = "twitch.reward.";

    // Routing keys
    public const string RewardRedeemed = RewardPrefix + "redeemed";
    public const string RewardEventsPattern = "twitch.reward.#";
    public const string UserJoined = "twitch.user.joined";
    public const string MessageReceived = "twitch.message.received";
    public const string MessageDeleted = "twitch.message.deleted";

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

    // Reward-specific routing keys
    public const string RewardConfetti = RewardPrefix + "confetti";
    public const string RewardFireworks = RewardPrefix + "fireworks";
    public const string RewardPhonkEdit = RewardPrefix + "phonkedit";
    public const string RewardTikTokEdit = RewardPrefix + "tiktokedit";
    public const string RewardRandomArt = RewardPrefix + "randomart";
    public const string RewardMikuMikuBeam = RewardPrefix + "mikumikubeam";
    public const string RewardGaoAlert = RewardPrefix + "gaoalert";
    public const string RewardWhat = RewardPrefix + "what";
    public const string RewardPedro = RewardPrefix + "pedro";
    public const string RewardTyazhelo = RewardPrefix + "tyazhelo";
    public const string RewardMichaelTime = RewardPrefix + "michaeltime";
    public const string RewardLegBum = RewardPrefix + "legbum";
    public const string RewardFumoFridayNight = RewardPrefix + "fumofridaynight";
    public const string RewardStone = RewardPrefix + "stone";
    public const string RewardCringe = RewardPrefix + "cringe";
    public const string RewardHello = RewardPrefix + "hello";
    public const string RewardBye = RewardPrefix + "bye";
    public const string RewardIntelligence = RewardPrefix + "intelligence";
    public const string RewardMikuScreamer = RewardPrefix + "mikuscreamer";
    public const string RewardSkibidibop = RewardPrefix + "skibidibop";
    public const string RewardAga = RewardPrefix + "aga";
    public const string RewardBadToBone = RewardPrefix + "badtobone";
    public const string RewardDanceDance = RewardPrefix + "dancedance";
    public const string RewardWednsdayFrog = RewardPrefix + "wednsdayfrog";
    public const string RewardEdge0100Alert = RewardPrefix + "edge0100alert";
    public const string RewardSkibidibopLong = RewardPrefix + "skibidiboplong";
    public const string RewardStatusQuestion = RewardPrefix + "statusquestion";
    public const string RewardCinemaRequest = RewardPrefix + "cinemarequest";
    public const string RewardAdhdSuperpower = RewardPrefix + "adhdsuperpower";
    public const string RewardCloseGame = RewardPrefix + "closegame";
    public const string RewardSelectGame = RewardPrefix + "selectgame";
    public const string RewardCredits = RewardPrefix + "credits";
    public const string RewardAllRefund = RewardPrefix + "allrefund";

    /// <summary>
    /// Все routing key'ы конкретных наград. <see cref="RewardRedeemed"/> сюда намеренно
    /// не входит: общий поток <c>twitch.reward.redeemed</c> разбирает отдельный
    /// <c>TwitchMediaAlerts</c>, а биндинг по <c>twitch.reward.#</c> заставил бы
    /// RewardAlertConsumer получать (и отбрасывать) те же сообщения повторно.
    /// </summary>
    public static readonly string[] RewardSpecificKeys =
    [
        RewardConfetti,
        RewardFireworks,
        RewardPhonkEdit,
        RewardTikTokEdit,
        RewardRandomArt,
        RewardMikuMikuBeam,
        RewardGaoAlert,
        RewardWhat,
        RewardPedro,
        RewardTyazhelo,
        RewardMichaelTime,
        RewardLegBum,
        RewardFumoFridayNight,
        RewardStone,
        RewardCringe,
        RewardHello,
        RewardBye,
        RewardIntelligence,
        RewardMikuScreamer,
        RewardSkibidibop,
        RewardAga,
        RewardBadToBone,
        RewardDanceDance,
        RewardWednsdayFrog,
        RewardEdge0100Alert,
        RewardSkibidibopLong,
        RewardStatusQuestion,
        RewardCinemaRequest,
        RewardAdhdSuperpower,
        RewardCloseGame,
        RewardSelectGame,
        RewardCredits,
        RewardAllRefund,
    ];

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

    /// <summary>
    /// Строит routing key награды из её названия в Twitch: регистр и все символы кроме
    /// букв/цифр отбрасываются. Используется и TwitchCore (публикация), и тестами,
    /// чтобы обе стороны гарантированно совпадали с routing key'ами из
    /// <see cref="RewardSpecificKeys"/>.
    /// </summary>
    public static string RewardKey(string rewardName)
    {
        var normalized = new StringBuilder(rewardName.Length);

        foreach (var symbol in rewardName)
        {
            if (char.IsLetterOrDigit(symbol))
            {
                normalized.Append(char.ToLowerInvariant(symbol));
            }
        }

        return normalized.Length == 0 ? RewardRedeemed : RewardPrefix + normalized.ToString();
    }
}
