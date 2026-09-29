using MARS.Shared.Clients;
using MARS.TwitchCore.Services.AutoHello;

namespace MARS.TwitchCore.Services.AutoHello;

/// <summary>
/// Реализация <see cref="IAutoHelloService"/> поверх межсервисного клиента
/// MARS.WaifuGacha. Бизнес-логика (кулдаун 20 часов, годовщина, выбор фразы)
/// живёт во владельце супругов — в MARS.WaifuGacha.
/// </summary>
public class AutoHelloClient(IWaifuGachaClient waifuGachaClient) : IAutoHelloService
{
    public Task<string?> GetAutoHelloMessageAsync(string userId, string displayName) =>
        waifuGachaClient.GetAutoHelloMessageAsync(userId, displayName);
}
