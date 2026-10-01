using MARS.Shared.Messaging;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Events;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Services.Rewards;

public class TwitchMessagesPublisher(
    ITwitchClient client,
    IHostApplicationLifetime lifetime,
    ITwitchEventValidationService validator,
    IMarsEventBus eventBus
) : BackgroundService
{
    private readonly CancellationToken _token = lifetime.ApplicationStopping;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += ClientOnOnMessageReceived;
            client.OnMessageCleared += ClientOnOnMessageCleared;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            client.OnMessageReceived -= ClientOnOnMessageReceived;
            client.OnMessageCleared -= ClientOnOnMessageCleared;
        });

        return Task.CompletedTask;
    }

    private async Task ClientOnOnMessageCleared(object? sender, OnMessageClearedArgs args)
    {
        if (args.Channel.Equals(TwitchConstants.Channel, StringComparison.OrdinalIgnoreCase))
        {
            await eventBus.PublishAsync(
                RabbitMqConfig.MessageDeleted,
                new TwitchMessageDeletedEvent
                {
                    MessageId = args.TargetMessageId,
                    Channel = args.Channel,
                },
                _token
            );
        }
    }

    private async Task ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs args)
    {
        var vr = await validator
            .ForMessageReceived(args)
            .RequireChannel()
            .SkipBlacklisted()
            .ValidateWithResponseAsync(args.ChatMessage.Username);

        if (vr.IsInvalid)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(args.ChatMessage.CustomRewardId))
        {
            await eventBus.PublishAsync(
                RabbitMqConfig.MessageReceived,
                new ChatMessageEvent
                {
                    UserId = args.ChatMessage.UserId,
                    UserName = args.ChatMessage.Username,
                    Message = args.ChatMessage.Message,
                    IsModerator = args.ChatMessage.UserDetail.IsModerator,
                    IsVip = args.ChatMessage.UserDetail.IsVip,
                    IsBroadcaster = args.ChatMessage.UserId == TwitchConstants.ChannelId,
                    ChatColor = args.ChatMessage.HexColor,
                },
                _token
            );
        }
    }
}
