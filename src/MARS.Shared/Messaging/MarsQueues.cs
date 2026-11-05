namespace MARS.Shared.Messaging;

/// <summary>
/// Имена очередей и dead-letter очередей шины.
/// </summary>
/// <remarks>
/// <para>
/// Имена заданы явно, а не выведены из имени типа: очередь — это контракт
/// эксплуатации (её видно в RabbitMQ, по ней настраивают алерты и смотрят
/// глубину), и переименование класса не должно молча завести вторую очередь,
/// оставив старую накапливать сообщения.
/// </para>
/// <para>
/// Один слушатель — одна очередь. Общая очередь на несколько типов сообщений
/// означала бы, что падение одного обработчика останавливает соседей: у
/// Wolverine подтверждение выдаётся на сообщение, но поток очереди общий.
/// </para>
/// </remarks>
public static class MarsQueues
{
    // MARS.Alerts
    public const string AlertsMedia = "mars.alerts.media";
    public const string AlertsRewardDispatch = "mars.alerts.reward-dispatch";
    public const string AlertsCostMatch = "mars.alerts.cost-match";
    public const string AlertsRewardInput = "mars.alerts.reward-input";
    public const string AlertsChatUsers = "mars.alerts.chat-users";
    public const string AlertsTriggerWords = "mars.alerts.trigger-words";
    public const string AlertsRewardInputTriggerWords = "mars.alerts.reward-input-trigger-words";

    // MARS.TwitchCore
    public const string TwitchCoreChatSend = "mars.twitchcore.chat-send";

    // MARS.WaifuGacha
    public const string WaifuRollCooldown = "mars.waifu.roll-cooldown";
    public const string WaifuChatCooldown = "mars.waifu.chat-cooldown";

    /// <summary>
    /// Очередь для типа-слушателя. <c>null</c> — тип слушателем не является.
    /// </summary>
    /// <remarks>
    /// Это правило и есть весь механизм отбора: Wolverine спрашивает его для
    /// каждого найденного обработчика, и <c>null</c> означает «своего адреса у
    /// этого обработчика нет». Поиск по имени типа, а не по атрибуту, выбран
    /// потому, что имена очередей всё равно приходится держать рядом с именами
    /// классов, а лишний атрибут на каждом из девяти типов ничего не добавил бы.
    /// </remarks>
    public static string? QueueNameForHandlerType(Type handlerType) =>
        handlerType.Name switch
        {
            "TwitchMediaListener" => AlertsMedia,
            "RewardRedeemedListener" => AlertsRewardDispatch,
            "CostMatchedRewardListener" => AlertsCostMatch,
            "RewardInputMessageListener" => AlertsRewardInput,
            "ChatUserListener" => AlertsChatUsers,
            "TriggerWordAlertListener" => AlertsTriggerWords,
            "RewardInputTriggerWordListener" => AlertsRewardInputTriggerWords,
            "ChatSendListener" => TwitchCoreChatSend,
            "RollCooldownListener" => WaifuRollCooldown,
            "ChatCooldownListener" => WaifuChatCooldown,
            _ => null,
        };

    /// <summary>
    /// Dead-letter очередь сервиса. Имя выводится из имени сервиса, а не из
    /// типа-слушателя: разбирать отказы удобнее по владельцу, а не по каждому
    /// обработчику отдельно.
    /// </summary>
    public static string DeadLetterQueueForService(string serviceName)
    {
        var normalized = serviceName.Trim();
        const string prefix = "MARS.";
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[prefix.Length..];
        }

        return $"mars.{normalized.ToLowerInvariant()}.dlq";
    }
}
