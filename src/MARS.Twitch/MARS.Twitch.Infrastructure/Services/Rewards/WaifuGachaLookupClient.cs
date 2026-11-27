using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Twitch.Infrastructure.Services.Rewards;

namespace MARS.Twitch.Infrastructure.Services.Rewards;

/// <summary>
/// Реализация <see cref="IWaifuLookupService"/> поверх межсервисного клиента
/// MARS.WaifuGacha. Используется мини-игрой «Русская рулетка», чтобы узнать
/// имя супруга игрока. Владеет супругами MARS.WaifuGacha, поэтому запрос
/// адресован туда напрямую по internal API, а не через RabbitMQ-ответы.
/// </summary>
public class WaifuGachaLookupClient(IMarsGrpcClient grpcClient) : IWaifuLookupService
{
    public async Task<string?> GetWaifuNameForUserAsync(
        string twitchUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            return await grpcClient.WaifuGacha.GetWaifuNameForUserAsync(twitchUserId, ct);
        }
        catch (RpcException)
        {
            return null;
        }
    }
}
