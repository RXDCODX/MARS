using System.Diagnostics;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Options;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Services.Chat;

/// <summary>
/// Потребитель <c>twitch.chat.send</c>: единственная точка отправки сообщений в чат
/// для остальных микросервисов.
/// </summary>
/// <remarks>
/// MARS.WaifuGacha раньше поднимал собственное IRC/EventSub-подключение теми же
/// учётными данными, что и MARS.TwitchCore. Twitch закрывает более старое из двух
/// соединений, поэтому один из сервисов молча переставал получать события
/// (блокер №19 из аудита PR). Теперь WaifuGacha публикует
/// <see cref="ChatSendEvent"/>, а сообщение уходит через единственное соединение
/// этого сервиса.
/// </remarks>
public class TwitchChatSendConsumer(
    IOptions<RabbitMqOptions> options,
    ITwitchClient client,
    ILogger<TwitchChatSendConsumer> logger,
    IHostApplicationLifetime lifetime,
    ITwitchEventValidationService validator,
    IMarsEventBus eventBus
) : RabbitMqConsumerBase(options.Value, "twitchcore", QueueName, [RabbitMqConfig.RoutingKeys.ChatSend], logger)
{
    public const string QueueName = "twitchcore.chatsend";

    private const int MaxMessageLength = 500;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += ClientOnOnMessageReceived;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            client.OnMessageReceived -= ClientOnOnMessageReceived;
        });

        return base.ExecuteAsync(stoppingToken);
    }

    private async Task ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var startedAt = Stopwatch.GetTimestamp();

        MarsMetrics.TwitchEventsReceived.Add(1);

        try
        {
            var validation = await validator
                .ForMessageReceived(e)
                .RequireChannel()
                .SkipBlacklisted()
                .ValidateWithResponseAsync(e.ChatMessage.Username);

            if (validation.IsInvalid)
            {
                logger.LogDebug(
                    "Chat message from {UserName} was rejected: {Reason}",
                    e.ChatMessage.Username,
                    validation.FirstError
                );

                return;
            }

            var message = new ChatMessageEvent
            {
                UserId = e.ChatMessage.UserId,
                UserName = e.ChatMessage.Username,
                Message = e.ChatMessage.Message,
                Timestamp = DateTime.UtcNow,
                IsModerator = e.ChatMessage.UserDetail.IsModerator,
                IsVip = e.ChatMessage.UserDetail.IsVip,
                IsBroadcaster = string.Equals(
                    e.ChatMessage.UserId,
                    TwitchConstants.ChannelId,
                    StringComparison.Ordinal
                ),
                ChatColor = e.ChatMessage.HexColor,
                CustomRewardId = e.ChatMessage.CustomRewardId,
            };

            var routingKey = string.IsNullOrWhiteSpace(message.CustomRewardId)
                ? RabbitMqConfig.RoutingKeys.ChatReceived
                : RabbitMqConfig.RoutingKeys.ChatRewardInputReceived;

            await eventBus.PublishAsync(routingKey, message, lifetime.ApplicationStopping);

            logger.LogDebug(
                "Twitch chat message published: {RoutingKey}, user={UserName}",
                routingKey,
                message.UserName
            );
        }
        catch (OperationCanceledException)
            when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
            logger.LogDebug("Chat message publishing cancelled during shutdown");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish Twitch chat message from {UserName}",
                e.ChatMessage.Username
            );
        }
        finally
        {
            MarsMetrics.TwitchEventProcessingTime.Record(
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds
            );
        }
    }

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var chatSend = Deserialize<ChatSendEvent>(json);

        if (chatSend is null || string.IsNullOrWhiteSpace(chatSend.Message))
        {
            logger.LogWarning("Chat send on {RoutingKey} has no message, skipped", routingKey);
            return;
        }

        var channel = string.IsNullOrWhiteSpace(chatSend.Channel)
            ? TwitchConstants.Channel
            : chatSend.Channel.Trim();

        var normalized = chatSend
            .Message.Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Trim();

        while (normalized.Length > MaxMessageLength)
        {
            var splitAt = normalized.LastIndexOf(' ', MaxMessageLength);

            if (splitAt <= 0)
            {
                splitAt = MaxMessageLength;
            }

            await client.SendMessageAsync(channel, normalized[..splitAt]);
            normalized = normalized[splitAt..].TrimStart(' ');
        }

        if (normalized.Length > 0)
        {
            await client.SendMessageAsync(channel, normalized);
        }
    }
}
