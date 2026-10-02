using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Пользователь, для которого команда показывает коллекцию.
/// </summary>
/// <remarks>
/// <c>TwitchId</c> и <c>Login</c> заполнены не одновременно: если логин
/// передали, но пользователя в базе нет, вернётся только логин — по нему
/// команда сообщает «не найден». Иначе были бы неразличимы случаи
/// «пользователь не передан» и «пользователя нет».
/// </remarks>
internal sealed record CollectionCommandTarget(
    string? TwitchId,
    string? DisplayName,
    string? Login
);

/// <summary>
/// Определение пользователя для команд инвентаря.
/// </summary>
/// <remarks>
/// Twitch-адаптер передаёт объект пользователя, API и Telegram — строку с
/// именем. Строка требует разрешения в Twitch ID через справочник
/// <c>MARS.TwitchCore</c>: коллекции хранятся по ID.
/// </remarks>
internal static class CollectionTargetResolver
{
    public static async Task<CollectionCommandTarget> ResolveAsync(
        Dictionary<string, object> parameters,
        ITwitchUserClient twitchUserClient,
        CancellationToken cancellationToken
    )
    {
        var result = new CollectionCommandTarget(null, null, null);

        var login = TwitchCaller.ResolveLogin(parameters);

        if (login is not null)
        {
            var twitchId = await twitchUserClient.ResolveIdByLoginAsync(login, cancellationToken);
            result = new CollectionCommandTarget(twitchId, login, login);
        }
        else
        {
            var callerId = TwitchCaller.ResolveId(parameters);
            result = new CollectionCommandTarget(callerId, null, null);
        }

        return result;
    }
}
