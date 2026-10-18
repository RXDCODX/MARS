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
        "analytics:read:games",
        "bits:read",
        "channel:bot",
        "channel:edit:commercial",
        "channel:manage:ads",
        "channel:manage:broadcast",
        "channel:manage:clips",
        "channel:manage:extensions",
        "channel:manage:guest_star",
        "channel:manage:moderators",
        "channel:manage:polls",
        "channel:manage:predictions",
        "channel:manage:raids",
        "channel:manage:redemptions",
        "channel:manage:schedule",
        "channel:manage:videos",
        "channel:manage:vips",
        "channel:moderate",
        "channel:read:ads",
        "channel:read:charity",
        "channel:read:editors",
        "channel:read:goals",
        "channel:read:guest_star",
        "channel:read:hype_train",
        "channel:read:polls",
        "channel:read:predictions",
        "channel:read:redemptions",
        "channel:read:stream_key",
        "channel:read:subscriptions",
        "channel:read:vips",
        "clips:edit",
        "editor:manage:clips",
        "moderation:read",
        "moderator:manage:announcements",
        "moderator:manage:automod",
        "moderator:manage:automod_settings",
        "moderator:manage:banned_users",
        "moderator:manage:blocked_terms",
        "moderator:manage:chat_messages",
        "moderator:manage:chat_settings",
        "moderator:manage:guest_star",
        "moderator:manage:shield_mode",
        "moderator:manage:shoutouts",
        "moderator:manage:suspicious_users",
        "moderator:manage:unban_requests",
        "moderator:manage:warnings",
        "moderator:read:automod_settings",
        "moderator:read:banned_users",
        "moderator:read:blocked_terms",
        "moderator:read:chat_messages",
        "moderator:read:chat_settings",
        "moderator:read:chatters",
        "moderator:read:followers",
        "moderator:read:guest_star",
        "moderator:read:moderators",
        "moderator:read:shield_mode",
        "moderator:read:shoutouts",
        "moderator:read:suspicious_users",
        "moderator:read:unban_requests",
        "moderator:read:vips",
        "moderator:read:warnings",
        "user:bot",
        "user:edit",
        "user:edit:broadcast",
        "user:manage:blocked_users",
        "user:manage:chat_color",
        "user:manage:whispers",
        "user:read:blocked_users",
        "user:read:broadcast",
        "user:read:chat",
        "user:read:email",
        "user:read:emotes",
        "user:read:follows",
        "user:read:moderated_channels",
        "user:read:subscriptions",
        "user:read:whispers",
        "user:write:chat",
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
