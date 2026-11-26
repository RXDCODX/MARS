using Grpc.Net.ClientFactory;
using MARS.Platform.Infrastructure.Grpc.SoundRequest;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Platform.Infrastructure.Grpc.Tts;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

namespace MARS.Platform.Infrastructure.Grpc;

public interface IMarsGrpcClient
{
    ITelegramApi Telegram { get; }

    ISoundRequestApi SoundRequest { get; }

    ITtsApi Tts { get; }
}

public sealed class MarsGrpcClient(
    GrpcClientFactory clientFactory,
    IServiceIdentity serviceIdentity
) : IMarsGrpcClient
{
    private readonly Lazy<ITelegramApi> _telegram = new(() => new TelegramApi(clientFactory));
    private readonly Lazy<ISoundRequestApi> _soundRequest = new(() => new SoundRequestApi(clientFactory));
    private readonly Lazy<ITtsApi> _tts = new(() => new TtsApi(clientFactory));

    public ITelegramApi Telegram
    {
        get
        {
            ThrowIfSelf("Telegram");
            return _telegram.Value;
        }
    }

    public ISoundRequestApi SoundRequest
    {
        get
        {
            ThrowIfSelf("SoundRequest");
            return _soundRequest.Value;
        }
    }

    public ITtsApi Tts
    {
        get
        {
            ThrowIfSelf("Tts");
            return _tts.Value;
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