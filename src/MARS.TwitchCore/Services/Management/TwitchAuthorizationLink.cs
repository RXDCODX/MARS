namespace MARS.TwitchCore.Services.Management;

/// <summary>
/// Ссылка авторизации Twitch, которую администратору нужно открыть.
/// </summary>
/// <remarks>
/// Вынесено отдельно от нотификатора: правило построения ссылки не зависит от того,
/// кто её отправляет, и проверяется без Telegram и без gRPC — одним вызовом
/// <see cref="Build"/>. Внутри правила живёт и набор запрошенных прав: он тоже не
/// свойство доставки.
/// </remarks>
public static class TwitchAuthorizationLink
{
    /// <summary>
    /// Права, без которых сервису не обойтись: чтение и правка настроек канала,
    /// модерация, награды, аналитика, чат и управление рекламой.
    /// </summary>
    private static readonly string[] Scopes =
    [
        "analytics:read:extensions",
        "user:edit",
        "user:read:email",
        "moderator:read:banned_users",
        "moderator:read:chat_messages",
        "moderator:read:moderators",
        "moderator:read:vips",
        "moderator:read:warnings",
        "user:read:whispers",
        "clips:edit",
        "bits:read",
        "analytics:read:games",
        "user:edit:broadcast",
        "user:read:broadcast",
        "chat:read",
        "chat:edit",
        "channel:moderate",
        "channel:read:subscriptions",
        "whispers:read",
        "whispers:edit",
        "moderation:read",
        "channel:read:redemptions",
        "channel:edit:commercial",
        "channel:read:hype_train",
        "channel:read:stream_key",
        "channel:manage:extensions",
        "channel:manage:broadcast",
        "user:edit:follows",
        "channel:manage:redemptions",
        "channel:read:editors",
        "channel:manage:videos",
        "user:read:blocked_users",
        "user:manage:blocked_users",
        "user:read:subscriptions",
        "user:read:follows",
        "channel:manage:polls",
        "channel:manage:predictions",
        "channel:read:polls",
        "channel:read:predictions",
        "moderator:manage:automod",
        "channel:manage:schedule",
        "channel:read:goals",
        "moderator:read:automod_settings",
        "moderator:manage:automod_settings",
        "moderator:manage:banned_users",
        "moderator:read:blocked_terms",
        "moderator:manage:blocked_terms",
        "moderator:manage:chat_settings",
        "moderator:read:chat_settings",
        "channel:manage:chat_settings",
        "user:manage:chat_color",
        "channel:manage:moderators",
        "user:read:vips",
        "moderator:manage:vips",
        "user:manage:whispers",
        "channel:read:charity",
        "channel:read:chatters",
        "channel:read:shield_mode",
        "channel:manage:shoutouts",
        "channel:read:followers",
        "channel:read:guest_star",
        "user:read:followers",
        "channel:bot",
        "user:bot",
        "user:read:chat",
        "channel:manage:ads",
        "channel:read:ads",
        "channel:read:moderated_channels",
        "user:write:chat",
        "channel:read:emotes",
        "moderator:read:unban_requests",
        "moderator:manage:unban_requests",
        "moderator:read:suspicious_users",
    ];

    /// <summary>
    /// Собирает ссылку авторизации.
    /// </summary>
    /// <param name="clientId">Идентификатор приложения Twitch.</param>
    /// <param name="redirectUri">
    /// Адрес обработчика кода. Обязан совпадать с тем, который уйдёт в обмене, —
    /// Twitch сравнивает строки точно.
    /// </param>
    public static string Build(string clientId, string redirectUri) =>
        "https://id.twitch.tv/oauth2/authorize"
        + $"?response_type=code"
        + $"&client_id={clientId}"
        + $"&redirect_uri={redirectUri}"
        + $"&scope={string.Join('+', Scopes)}";
}
