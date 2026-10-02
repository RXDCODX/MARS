using Grpc.Core;

namespace MARS.Shared.Grpc;

/// <summary>
/// Заглушка для процессов, где настройки ADHD нет: <c>TelegramusGrpcService</c>
/// поднимается и в MARS.OBS, а таблица принадлежит MARS.Alerts.
/// </summary>
public sealed class UnavailableAdhdConfigStore : IAdhdConfigStore
{
    public const string Reason = "Настройка раскладки ADHD хранится в MARS.Alerts";

    public Task<string> GetAsync(CancellationToken cancellationToken)
    {
        throw new RpcException(new Status(StatusCode.FailedPrecondition, Reason));
    }

    public Task<string> UpdateAsync(string configJson, CancellationToken cancellationToken)
    {
        throw new RpcException(new Status(StatusCode.FailedPrecondition, Reason));
    }
}
