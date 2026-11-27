using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class SrlistCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "srlist";

    public override string Description => "Добавляет плейлист в очередь SoundRequest";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "tracksCount",
                Description = "Количество треков",
                Type = CommandParameterType.Int,
                Required = false,
                DefaultValue = "10",
            },
            new()
            {
                Name = "playlistQuery",
                Description = "URL или поисковый запрос плейлиста",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

    public override string[] Aliases => ["srlists"];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var userId = parameters.TryGetValue("userId", out var userValue) ? userValue?.ToString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CommandResult.Fail("Не удалось определить пользователя.", CommandErrorCode.BadArguments);
        }

        var tracksCount = 10;
        if (parameters.TryGetValue("tracksCount", out var countValue))
        {
            int.TryParse(countValue?.ToString(), out tracksCount);
        }
        if (tracksCount <= 0)
        {
            tracksCount = 10;
        }

        var playlistQuery = parameters.TryGetValue("playlistQuery", out var queryValue) ? queryValue?.ToString()?.Trim() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(playlistQuery))
        {
            return CommandResult.Fail("Не указан плейлист.", CommandErrorCode.BadArguments);
        }

        try
        {
            var text = await grpcClient.SoundRequest.AddPlaylistAsync(playlistQuery, userId, tracksCount, cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "Плейлист добавлен в очередь." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось добавить плейлист.", CommandErrorCode.TargetUnreachable);
        }
    }
}