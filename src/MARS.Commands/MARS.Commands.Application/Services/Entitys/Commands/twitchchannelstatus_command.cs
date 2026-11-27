using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Twitch.Control;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class TwitchchannelstatusCommand(IMarsGrpcClient grpcClient) : BaseCommand
{
    public override string CommandName => "twitchchannelstatus";

    public override string Description => "Показывает состояние подключения Twitch-чата";

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
            var text =
                $"EventSub: {(state.IsEventSubConnected ? "connected" : "disconnected")}\n"
                + $"Chat: {(state.IsTwitchChatConnected ? "connected" : "disconnected")}\n"
                + $"JoinedChannels: {(string.IsNullOrWhiteSpace(state.JoinedChannels) ? "-" : state.JoinedChannels)}\n"
                + $"LastConnectedAt: {(string.IsNullOrWhiteSpace(state.LastConnectedAt) ? "-" : state.LastConnectedAt)}\n"
                + $"LastDisconnectedAt: {(string.IsNullOrWhiteSpace(state.LastDisconnectedAt) ? "-" : state.LastDisconnectedAt)}\n"
                + $"LastDisconnectReason: {(string.IsNullOrWhiteSpace(state.LastDisconnectReason) ? "-" : state.LastDisconnectReason)}\n"
                + $"ReconnectAttempts: {state.ReconnectAttempts}\n"
                + $"IsReconnecting: {state.IsReconnecting}";

            return CommandResult.Ok(text);
        }
        catch (RpcException)
        {
            return CommandResult.Fail(
                "Не удалось получить статус Twitch.",
                CommandErrorCode.TargetUnreachable
            );
        }
    }
}