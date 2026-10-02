namespace MARS.Shared.Clients;

/// <summary>
/// Справочник пользователей Twitch во внутреннем API MARS.TwitchCore.
/// </summary>
/// <remarks>
/// Нужен командам, которые принимают имя пользователя, а хранят данные по
/// Twitch ID: коллекции лежат в <c>MARS.WaifuGacha</c> и ключ у них — ID.
/// </remarks>
public interface ITwitchUserClient
{
    /// <summary>
    /// Twitch ID по логину. null — пользователя нет в базе либо сервис
    /// недоступен; вызывающий различает эти случаи по <c>Success</c>
    /// конверта либо по факту вызова.
    /// </summary>
    Task<string?> ResolveIdByLoginAsync(
        string login,
        CancellationToken cancellationToken = default
    );
}
