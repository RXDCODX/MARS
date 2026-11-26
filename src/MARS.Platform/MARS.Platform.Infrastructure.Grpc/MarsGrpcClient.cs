using Grpc.Net.ClientFactory;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

namespace MARS.Platform.Infrastructure.Grpc;

public interface IMarsGrpcClient
{
    ITelegramApi Telegram { get; }
}

public sealed class MarsGrpcClient(
    GrpcClientFactory clientFactory,
    IServiceIdentity serviceIdentity
) : IMarsGrpcClient
{
    private readonly Lazy<ITelegramApi> _telegram = new(() => new TelegramApi(clientFactory));

    public ITelegramApi Telegram
    {
        get
        {
            ThrowIfSelf("Telegram");
            return _telegram.Value;
        }
    }

    private void ThrowIfSelf(string serviceKey)
    {
        if (serviceIdentity.IsSelf(serviceKey))
        {
            throw new InvalidOperationException(
                $"Service '{serviceIdentity.LogicalName}' cannot call gRPC service '{serviceKey}' (self-call)."
            );
        }
    }
}