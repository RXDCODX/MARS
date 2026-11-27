using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Domain.Entities;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Control;
using MARS.Shared.Kernel;

namespace MARS.Commands.Host.Services;

public sealed class GrpcCommandResponseSender(IMarsGrpcClient grpcClient) : ICommandResponseSender
{
    public async Task SendAsync(CommandExecution execution, string responsePayload, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<CommandResponsePayload>(responsePayload);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Text))
        {
            return;
        }

        switch (payload.Platform)
        {
            case "Twitch":
                if (!string.IsNullOrWhiteSpace(payload.ReplyTo))
                {
                    await grpcClient.Twitch.SendChatMessageAsync(
                        new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
                        {
                            Channel = payload.ReplyTo,
                            Message = payload.Text,
                            ReplyTo = payload.SourceMessageId ?? string.Empty,
                        },
                        cancellationToken
                    );
                }
                break;

            case "Discord":
                if (ulong.TryParse(payload.ReplyTo, out var discordChannelId))
                {
                    await grpcClient.Discord.SendMessageAsync(
                        discordChannelId.ToString(),
                        payload.Text,
                        cancellationToken
                    );
                }
                break;

            case "Telegram":
                if (long.TryParse(payload.ReplyTo, out var telegramChatId))
                {
                    await grpcClient.Telegram.Messages.SendAsync(
                        telegramChatId,
                        payload.Text,
                        cancellationToken
                    );
                }
                break;
        }
    }
}
