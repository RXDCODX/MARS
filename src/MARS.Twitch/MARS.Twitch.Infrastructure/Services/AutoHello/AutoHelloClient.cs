using MARS.Platform.Infrastructure.Http;
using MARS.Twitch.Infrastructure.Services.AutoHello;
using MARS.WaifuGacha.Models.Rest;

namespace MARS.Twitch.Infrastructure.Services.AutoHello;

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
