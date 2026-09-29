using MARS.Shared.Clients;
using MARS.TwitchCore.Services.Rewards;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Реализация <see cref="IWaifuLookupService"/> поверх межсервисного клиента
/// MARS.WaifuGacha. Используется мини-игрой «Русская рулетка», чтобы узнать
/// имя супруга игрока. Владеет супругами MARS.WaifuGacha, поэтому запрос
/// адресован туда напрямую по internal API, а не через RabbitMQ-ответы.
/// </summary>
public class WaifuGachaLookupClient(IWaifuGachaClient waifuGachaClient) : IWaifuLookupService
{
    public Task<string?> GetWaifuNameForUserAsync(
        string twitchUserId,
        CancellationToken ct = default
    ) => waifuGachaClient.GetWaifuNameForUserAsync(twitchUserId, ct);
}
