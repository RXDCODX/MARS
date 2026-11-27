using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

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
        IMarsGrpcClient grpcClient,
        CancellationToken cancellationToken
    )
    {
        var result = new CollectionCommandTarget(null, null, null);

        var login = TwitchCaller.ResolveLogin(parameters);

        if (login is not null)
        {
            string? twitchId = null;
            try
            {
                var user = await grpcClient.Twitch.GetFullUserByLoginAsync(
                    login,
                    cancellationToken
                );
                twitchId = user?.TwitchId;
            }
            catch (RpcException)
            {
                twitchId = null;
            }
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
