using Grpc.Net.ClientFactory;
using MARS.Platform.Infrastructure.Grpc.Alerts;
using MARS.Platform.Infrastructure.Grpc.Commands;
using MARS.Platform.Infrastructure.Grpc.MediaStorage;
using MARS.Platform.Infrastructure.Grpc.Shikimori;
using MARS.Platform.Infrastructure.Grpc.Discord;
using MARS.Platform.Infrastructure.Grpc.SoundRequest;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Platform.Infrastructure.Grpc.Twitch;
using MARS.Platform.Infrastructure.Grpc.Tts;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

namespace MARS.Platform.Infrastructure.Grpc;

public interface IMarsGrpcClient
{
    IAlertsApi Alerts { get; }

    ICommandsApi Commands { get; }

    ITwitchApi Twitch { get; }

    IMediaStorageApi MediaStorage { get; }

    IShikimoriApi Shikimori { get; }

    IDiscordApi Discord { get; }

    ITelegramApi Telegram { get; }

    ISoundRequestApi SoundRequest { get; }

    ITtsApi Tts { get; }
}

public sealed class MarsGrpcClient(
    GrpcClientFactory clientFactory,
    IServiceIdentity serviceIdentity
) : IMarsGrpcClient
{
    private readonly Lazy<IAlertsApi> _alerts = new(() => new AlertsApi(clientFactory));
    private readonly Lazy<ICommandsApi> _commands = new(() => new CommandsApi(clientFactory));
    private readonly Lazy<IMediaStorageApi> _mediaStorage = new(() => new MediaStorageApi(clientFactory));
    private readonly Lazy<IShikimoriApi> _shikimori = new(() => new ShikimoriApi(clientFactory));
    private readonly Lazy<IDiscordApi> _discord = new(() => new DiscordApi(clientFactory));
    private readonly Lazy<ITelegramApi> _telegram = new(() => new TelegramApi(clientFactory));
    private readonly Lazy<ITwitchApi> _twitch = new(() => new TwitchApi(clientFactory));
    private readonly Lazy<ISoundRequestApi> _soundRequest = new(() => new SoundRequestApi(clientFactory));
    private readonly Lazy<ITtsApi> _tts = new(() => new TtsApi(clientFactory));

    public IAlertsApi Alerts
    {
        get
        {
            ThrowIfSelf("Alerts");
            return _alerts.Value;
        }
    }

    public ICommandsApi Commands
    {
        get
        {
            ThrowIfSelf("Commands");
            return _commands.Value;
        }
    }

    public IMediaStorageApi MediaStorage
    {
        get
        {
            ThrowIfSelf("MediaStorage");
            return _mediaStorage.Value;
        }
    }

    public IShikimoriApi Shikimori
    {
        get
        {
            ThrowIfSelf("Shikimori");
            return _shikimori.Value;
        }
    }

    public IDiscordApi Discord
    {
        get
        {
            ThrowIfSelf("Discord");
            return _discord.Value;
        }
    }

    public ITelegramApi Telegram
    {
        get
        {
            ThrowIfSelf("Telegram");
            return _telegram.Value;
        }
    }

    public ITwitchApi Twitch
    {
        get
        {
            ThrowIfSelf("TwitchCore");
            return _twitch.Value;
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