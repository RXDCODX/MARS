namespace MARS.Shared.Messaging;

/// <summary>
/// Топология RabbitMQ: вместо литералов в сервисах — две группы имён.
/// </summary>
/// <remarks>
/// <para>
/// Очередь называется <c>mars.&lt;сервис&gt;.&lt;назначение&gt;</c>: в журнале
/// брокера сразу видно, кто её читает (<c>mars.alerts.chat-users</c> против
/// прежнего <c>alerts.chatusers</c>, где не было ни владельца, ни разделителя
/// слов).
/// </para>
/// <para>
/// Routing key называется <c>&lt;сущность&gt;.&lt;действие&gt;</c>: сервис-владелец
/// в ключе не участвует, потому что одно событие может порождаться и читаться
/// несколькими сервисами. События — причастие (<c>received</c>, <c>deleted</c>,
/// <c>activated</c>), команды — повелительное наклонение (<c>send</c>,
/// <c>refund</c>).
/// </para>
/// </remarks>
public static class RabbitMqConfig
{
    public const string SectionName = "RabbitMq";
    public const string ExchangeName = "mars.events";

    /// <summary>Имена очередей: <c>mars.&lt;сервис&gt;.&lt;назначение&gt;</c>.</summary>
    public static class Queues
    {
        /// <summary>Исходные Twitch EventSub notifications для оверлеев.</summary>
        public const string AlertsEventSub = "mars.alerts.eventsub";

        /// <summary>Поток сообщений чата для учёта зрителей.</summary>
        public const string AlertsChatUsers = "mars.alerts.chat-users";

        /// <summary>Сообщения чата для триггерных алертов.</summary>
        public const string AlertsTriggerWords = "mars.alerts.trigger-words";

        /// <summary>Просьбы отправить сообщение в чат через единственное IRC-подключение.</summary>
        public const string TwitchCoreChatSend = "mars.twitch-core.chat-send";

        /// <summary>Просьбы вернуть баллы Twitch за отклонённый редемпшн.</summary>
        public const string TwitchCoreRewardRefund = "mars.twitch-core.reward-refund";

        /// <summary>Активации наград, продлевающие кулдаун роллов.</summary>
        public const string WaifuGachaRollCooldowns = "mars.waifu-gacha.roll-cooldowns";
    }

    /// <summary>
    /// Routing key'и: <c>&lt;сущность&gt;.&lt;действие&gt;</c>.
    /// </summary>
    public static class RoutingKeys
    {
        /// <summary>
        /// Исходный фрейм Twitch EventSub. Публикует владелец подключения
        /// (MARS.TwitchCore), читают оверлеи и обработчики наград.
        /// </summary>
        public const string EventSubReceived = "eventsub.received";

        /// <summary>Сообщение чата.</summary>
        public const string ChatReceived = "chat.received";

        /// <summary>
        /// Сообщение, отправленное через награду с вводом текста. Отдельный ключ,
        /// потому что в оверлей чата такое сообщение не попадает, но по нему
        /// поднимается привязанный к награде алерт (AC.S08): подмешивать такие
        /// сообщения в <see cref="ChatReceived"/> нельзя — потребители обычного
        /// чата ответили бы на награду как на обычное сообщение.
        /// </summary>
        public const string ChatRewardInputReceived = "chat.reward-input.received";

        /// <summary>Сообщение чата удалено.</summary>
        public const string ChatDeleted = "chat.deleted";

        /// <summary>
        /// Запрос на отправку сообщения в чат. Владелец единственного IRC-соединения —
        /// MARS.TwitchCore, поэтому остальные сервисы не поднимают своё подключение
        /// (иначе Twitch отключает более старое из двух), а публикуют это событие.
        /// </summary>
        public const string ChatSend = "chat.send";

        /// <summary>
        /// Просьба вернуть поинты за редемпшн. Публикуют те, у кого нет токена
        /// Twitch (MARS.Alerts), исполняет владелец токена (MARS.TwitchCore).
        /// </summary>
        public const string RewardRefund = "reward.refund";

        /// <summary>
        /// Редемпшн награды канала. Публикует владелец EventSub-подключения
        /// (MARS.TwitchCore), читают сервисы, которым нужен факт траты: например
        /// MARS.WaifuGacha продлевает кулдаун ролла. Что именно активировано,
        /// видно по <c>AlertKey</c> в теле.
        /// </summary>
        public const string RewardActivated = "reward.activated";
    }
}
