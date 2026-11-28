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
public class JoinedtwitchchannelsCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "joinedtwitchchannels";

    public override string Description => "Показывает каналы, к которым подключён Twitch-бот";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var state = await grpcClient.Twitch.GetConnectionStateAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(state.JoinedChannels))
            {
                return CommandResult.Ok("Бот не подключён ни к одному каналу.");
            }

            return CommandResult.Ok($"Подключённые каналы: {state.JoinedChannels}");
        }
        catch (RpcException)
        {
            return CommandResult.Fail("Не удалось получить список каналов.", CommandErrorCode.TargetUnreachable);
        }
    }
}