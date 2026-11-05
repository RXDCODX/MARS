using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using Wolverine;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Единственный публикатор событий чата.
/// </summary>
/// <remarks>
/// Заменяет <c>TwitchMessagesHubAwaker</c> из монолита. Сообщение уходит
/// двумя путями: в брокер — для <c>MARS.Alerts</c> (триггерные алерты и учёт
/// участников чата), и в <c>ITelegramusNotifier</c> — прямо в оверлей.
/// Публикация идёт через <see cref="IMessageBus"/>: тип сообщения определяет
/// обменник и очередь,routing key задавать не нужно.
/// </remarks>
public class TwitchMessagesPublisher(
    ITwitchClient client,
    IHostApplicationLifetime lifetime,
    ITwitchEventValidationService validator,
    IMessageBus messageBus,
    ITelegramusNotifier notifier
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
            await messageBus.PublishAsync(
                new ChatMessageDeletedEvent
                {
                    MessageId = args.TargetMessageId,
                    Channel = args.Channel,
                    DeletedAt = DateTime.UtcNow,
                }
            );

            await notifier.DeleteMessage(args.TargetMessageId);
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

        var chatMessage = new ChatMessageReceivedEvent
        {
            UserId = args.ChatMessage.UserId,
            UserName = args.ChatMessage.Username,
            Message = args.ChatMessage.Message,
            IsModerator = args.ChatMessage.UserDetail.IsModerator,
            IsVip = args.ChatMessage.UserDetail.IsVip,
            IsBroadcaster = args.ChatMessage.UserId == TwitchConstants.ChannelId,
            ChatColor = args.ChatMessage.HexColor,
            CustomRewardId = args.ChatMessage.CustomRewardId,
            Timestamp = DateTime.UtcNow,
        };

        if (string.IsNullOrWhiteSpace(args.ChatMessage.CustomRewardId))
        {
            await messageBus.PublishAsync(chatMessage);

            // Оверлей получает сообщение и по брокерному пути, и напрямую:
            // сообщение в чате рисуется сразу, не дожидаясь обработки.
            await notifier.NewMessage(args.ChatMessage.Id, chatMessage);
        }
        else
        {
            // Сообщение через награду в оверлей чата не попадает — так же и в
            // монолите. Но по нему поднимается привязанный к награде алерт,
            // поэтому отдельный тип: потребители обычного чата таких сообщений
            // видеть не должны.
            await messageBus.PublishAsync(
                new RewardInputMessageEvent
                {
                    UserId = args.ChatMessage.UserId,
                    UserName = args.ChatMessage.Username,
                    Message = args.ChatMessage.Message,
                    IsModerator = args.ChatMessage.UserDetail.IsModerator,
                    IsVip = args.ChatMessage.UserDetail.IsVip,
                    IsBroadcaster = args.ChatMessage.UserId == TwitchConstants.ChannelId,
                    ChatColor = args.ChatMessage.HexColor,
                    CustomRewardId = args.ChatMessage.CustomRewardId,
                    Timestamp = DateTime.UtcNow,
                }
            );
        }
    }
}
