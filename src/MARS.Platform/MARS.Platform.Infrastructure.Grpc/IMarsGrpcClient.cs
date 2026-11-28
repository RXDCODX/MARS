using System.Diagnostics.CodeAnalysis;
using MARS.Platform.Infrastructure.Grpc.Alerts;
using MARS.Platform.Infrastructure.Grpc.Commands;
using MARS.Platform.Infrastructure.Grpc.CommandsCatalog;
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
    IWaifuGachaApi WaifuGacha { get; }

    IAlertsApi Alerts { get; }

    ICommandsApi Commands { get; }

    ICommandsCatalogApi CommandsCatalog { get; }

    ITwitchApi Twitch { get; }

    IMediaStorageApi MediaStorage { get; }

    IShikimoriApi Shikimori { get; }

    IDiscordApi Discord { get; }

    ITelegramApi Telegram { get; }

    ISoundRequestApi SoundRequest { get; }

    ITtsApi Tts { get; }
}
