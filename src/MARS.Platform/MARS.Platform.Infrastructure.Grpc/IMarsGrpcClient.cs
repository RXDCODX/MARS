using MARS.Platform.Infrastructure.Grpc.Alerts;
using MARS.Platform.Infrastructure.Grpc.AudioController;
using MARS.Platform.Infrastructure.Grpc.Commands;
using MARS.Platform.Infrastructure.Grpc.Discord;
using MARS.Platform.Infrastructure.Grpc.MediaStorage;
using MARS.Platform.Infrastructure.Grpc.Shikimori;
using MARS.Platform.Infrastructure.Grpc.SoundRequest;
using MARS.Platform.Infrastructure.Grpc.Telegram;
using MARS.Platform.Infrastructure.Grpc.Tts;
using MARS.Platform.Infrastructure.Grpc.Twitch;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;

namespace MARS.Platform.Infrastructure.Grpc;

public interface IMarsGrpcClient
{
    IAudioControllerApi AudioController { get; }

    IWaifuGachaApi WaifuGacha { get; }

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
