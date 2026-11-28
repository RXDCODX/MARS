using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class SrCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "sr";

    public override string Description => "Добавляет трек в очередь SoundRequest";

    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms => [Platform.Twitch, Platform.Api];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "query",
                Description = "URL или поисковый запрос",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

    public override string[] Aliases => ["soundrequest"];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var query = parameters.TryGetValue("query", out var queryValue) ? queryValue?.ToString()?.Trim() ?? string.Empty : string.Empty;
        var userId = parameters.TryGetValue("userId", out var userValue) ? userValue?.ToString() ?? string.Empty : string.Empty;

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(userId))
        {
            return CommandResult.Fail("Не указан трек или пользователь.", CommandErrorCode.BadArguments);
        }

        try
        {
            var text = await grpcClient.SoundRequest.AddTrackAsync(query, userId, cancellationToken);
            return CommandResult.Ok(string.IsNullOrWhiteSpace(text) ? "Трек добавлен в очередь." : text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось добавить трек.", CommandErrorCode.TargetUnreachable);
        }
    }
}