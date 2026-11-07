using MARS.Platform.Clients.Http.Infrastructure;

namespace MARS.Commands.Services.Entitys.Commands;

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
