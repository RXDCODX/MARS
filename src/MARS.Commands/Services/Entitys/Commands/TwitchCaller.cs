namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Извлечение Twitch ID вызывающего из параметров команды.
/// </summary>
/// <remarks>
/// Платформенные адаптеры передают разное: Twitch кладёт объект
/// <c>TwitchUser</c>, API и Telegram — строку в <c>userId</c>. Без общей
/// выборки каждая команда писала бы это заново и работала бы на одной
/// платформе.
/// </remarks>
internal static class TwitchCaller
{
    /// <summary>
    /// Twitch ID вызывающего либо <c>null</c>, если определить не удалось.
    /// </summary>
    public static string? ResolveId(Dictionary<string, object> parameters)
    {
        string? result = null;

        if (parameters.TryGetValue("userId", out var userId) && userId is string id)
        {
            result = id;
        }
        else if (parameters.TryGetValue("user", out var user) && user is not null)
        {
            result = ExtractTwitchId(user);
        }

        return result;
    }

    /// <summary>
    /// Имя пользователя из строки параметра: адаптеры передают логин с
    /// ведущим <c>@</c>, а API Shikimori и Twitch его не ждут.
    /// </summary>
    public static string? ResolveLogin(Dictionary<string, object> parameters)
    {
        string? result = null;

        if (
            parameters.TryGetValue("displayName", out var displayName)
            && displayName is string login
            && !string.IsNullOrWhiteSpace(login)
        )
        {
            result = login.Trim().TrimStart('@');
        }

        return result;
    }

    private static string? ExtractTwitchId(object user)
    {
        string? result = null;

        var property = user.GetType().GetProperty("TwitchId");

        if (property?.GetValue(user) is string twitchId)
        {
            result = twitchId;
        }

        return result;
    }
}
