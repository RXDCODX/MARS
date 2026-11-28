using System;
using System.Diagnostics.CodeAnalysis;
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
using ProtoDiscord = MARS.Shared.Contracts.Discord.Control;

namespace MARS.Commands.Host.Services;

[ExcludeFromCodeCoverage]
public sealed class GrpcCommandResponseSender(IMarsGrpcClient grpcClient) : ICommandResponseSender
{
    public async Task SendAsync(
        CommandExecution execution,
        string responsePayload,
        CancellationToken cancellationToken
    )
    {
        var payload = JsonSerializer.Deserialize<CommandResponsePayload>(responsePayload);
        if (
            payload is null
            || (
                string.IsNullOrWhiteSpace(payload.Text)
                && (payload.Attachments is null || payload.Attachments.Count == 0)
            )
        )
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
                    if (payload.Attachments is { Count: > 0 })
                    {
                        foreach (var attachment in payload.Attachments)
                        {
                            await grpcClient.Discord.SendFileAsync(
                                new ProtoDiscord.DiscordFileSendRequest
                                {
                                    ChannelId = discordChannelId.ToString(),
                                    FileUrl = attachment.Url,
                                    FileName = attachment.FileName ?? "file",
                                    MimeType = attachment.MimeType ?? string.Empty,
                                    Caption = attachment.Caption ?? payload.Text,
                                    ReplyToMessageId = long.TryParse(
                                        payload.SourceMessageId,
                                        out var replyToMessageId
                                    )
                                        ? replyToMessageId
                                        : 0,
                                    MediaKind = (int)attachment.Kind,
                                },
                                cancellationToken
                            );
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(payload.Text))
                    {
                        await grpcClient.Discord.SendMessageAsync(
                            discordChannelId.ToString(),
                            payload.Text,
                            cancellationToken
                        );
                    }
                }
                break;

            case "Telegram":
                if (long.TryParse(payload.ReplyTo, out var telegramChatId))
                {
                    var attachment = payload.Attachments?.FirstOrDefault();
                    if (attachment is not null)
                    {
                        var replyToMessageId = long.TryParse(
                            payload.SourceMessageId,
                            out var parsedReplyId
                        )
                            ? parsedReplyId
                            : 0L;

                        await grpcClient.Telegram.Messages.SendMediaAsync(
                            telegramChatId,
                            attachment.Url,
                            attachment.FileName ?? string.Empty,
                            attachment.MimeType ?? string.Empty,
                            attachment.Caption ?? payload.Text,
                            replyToMessageId,
                            (int)attachment.Kind,
                            attachment.DurationSeconds ?? 0,
                            attachment.Width ?? 0,
                            attachment.Height ?? 0,
                            cancellationToken
                        );
                    }
                    else
                    {
                        await grpcClient.Telegram.Messages.SendAsync(
                            telegramChatId,
                            payload.Text,
                            cancellationToken
                        );
                    }
                }
                break;
        }
    }
}
