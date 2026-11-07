using MARS.Platform.Grpc.Infrastructure.Notifications;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Единственный публикатор событий чата.
/// </summary>
/// <remarks>
/// Заменяет <c>TwitchMessagesHubAwaker</c> из монолита. Сообщение уходит
/// двумя путями: в RabbitMQ — для <c>MARS.Alerts</c> (триггерные алерты и учёт
/// участников чата), и в <c>ITelegramusNotifier</c> — прямо в оверлей.
/// </remarks>
public class TwitchMessagesPublisher(
    ITwitchClient client,
    IHostApplicationLifetime lifetime,
    ITwitchEventValidationService validator,
    IMarsEventBus eventBus,
    IMarsWolverineEventBus wolverineEventBus,
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
            var deletedEvent = new TwitchMessageDeletedEvent
            {
                MessageId = args.TargetMessageId,
                Channel = args.Channel,
            };

            // Legacy consumers keep receiving their existing routing key.
            await eventBus.PublishAsync(RabbitMqConfig.MessageDeleted, deletedEvent, _token);

            // New Wolverine listeners receive the typed contract on its type-derived exchange.
            await wolverineEventBus.PublishAsync(deletedEvent, _token);

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

        var chatMessage = new ChatEventMessageEvent
        {
            UserId = args.ChatMessage.UserId,
            UserName = args.ChatMessage.Username,
            Message = args.ChatMessage.Message,
            IsModerator = args.ChatMessage.UserDetail.IsModerator,
            IsVip = args.ChatMessage.UserDetail.IsVip,
            IsBroadcaster = args.ChatMessage.UserId == TwitchConstants.ChannelId,
            ChatColor = args.ChatMessage.HexColor,
            CustomRewardId = args.ChatMessage.CustomRewardId,
            Bits = args.ChatMessage.Bits,
            BitsInDollars = args.ChatMessage.BitsInDollars,
            Channel = args.ChatMessage.Channel,
            EmoteReplacedMessage = args.ChatMessage.EmoteReplacedMessage,
            IsFirstMessage = args.ChatMessage.IsFirstMessage,
            IsHighlighted = args.ChatMessage.IsHighlighted,
            RoomId = args.ChatMessage.RoomId,
            BotUsername = args.ChatMessage.BotUsername,
            TmiSent = args.ChatMessage.TmiSent,
            IsSkippingSubMode = args.ChatMessage.IsSkippingSubMode,
            DisplayName = args.ChatMessage.DisplayName,
        };

        if (string.IsNullOrWhiteSpace(args.ChatMessage.CustomRewardId))
        {
            // Legacy consumers keep receiving their existing routing key.
            await eventBus.PublishAsync(RabbitMqConfig.MessageReceived, chatMessage, _token);

            // New Wolverine listeners receive the typed message on a separate exchange.
            await wolverineEventBus.PublishAsync(chatMessage, _token);

            // Оверлей получает сообщение и по RabbitMQ-пути, и напрямую:
            // сообщение в чате рисуется сразу, не дожидаясь обработки.
            await notifier.NewMessage(args.ChatMessage.Id, chatMessage);
        }
        else
        {
            // Сообщение через награду в оверлей чата не попадает — так же и в
            // монолите. Но по нему поднимается привязанный к награде алерт,
            // поэтому отдельный ключ: потребители обычного чата таких сообщений
            // видеть не должны.
            // Legacy consumers keep receiving their existing routing key and payload.
            await eventBus.PublishAsync(RabbitMqConfig.RewardInputMessage, chatMessage, _token);

            // New Wolverine consumers use a dedicated message contract and queue.
            await wolverineEventBus.PublishAsync(
                new TwitchRewardInputMessageEvent { Message = chatMessage },
                _token
            );
        }
    }
}
