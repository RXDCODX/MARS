using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class MikuMondayRewardCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "mikumonday";
    public override string Description =>
        "Ручная выдача Miku Monday: без очереди, по нику (или стример без параметра)";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "nickname",
                Description = "Никнейм пользователя Twitch. Если не указан — считается стримером",
                Type = CommandParameterType.String,
                Required = false,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var nickname = parameters.TryGetValue("nickname", out var n) ? n as string : null;
        var isStreamer = string.IsNullOrWhiteSpace(nickname);
        var twitchId = isStreamer
            ? (TwitchCaller.ResolveId(parameters) ?? "streamer")
            : nickname!.TrimStart('@');

        MARS.Shared.Contracts.Clients.MikuMondayResult? result;
        try
        {
            result = await grpcClient.WaifuGacha.PickMikuMondayTrackAsync(
                twitchId,
                twitchId,
                isStreamer,
                cancellationToken
            );
        }
        catch (RpcException)
        {
            result = null;
        }

        if (result is null || result.Track is null)
        {
            return CommandResult.Fail(
                result?.Error ?? "Не удалось выполнить Miku Monday.",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(
            $"Miku Monday: трек #{result.Track.Number} — {result.Track.Artist} - {result.Track.Title}"
        );
    }
}
