using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Twitch.Infrastructure.Services.AutoHello;

namespace MARS.Twitch.Infrastructure.Services.AutoHello;

/// <summary>
/// Реализация <see cref="IAutoHelloService"/> поверх межсервисного клиента
/// MARS.WaifuGacha. Бизнес-логика (кулдаун 20 часов, годовщина, выбор фразы)
/// живёт во владельце супругов — в MARS.WaifuGacha.
/// </summary>
public class AutoHelloClient(IMarsGrpcClient grpcClient) : IAutoHelloService
{
    public async Task<string?> GetAutoHelloMessageAsync(string userId, string displayName)
    {
        try
        {
            return await grpcClient.WaifuGacha.GetAutoHelloMessageAsync(userId, displayName);
        }
        catch (RpcException)
        {
            return null;
        }
    }
}
