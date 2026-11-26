using MARS.Twitch.Application.Models;
using MARS.Twitch.Application.Services;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace MARS.Twitch.Infrastructure.Services.Inbox;

/// <summary>
/// Фоновая служба, записывающая все входящие сообщения чата Twitch в общий стор.
/// </summary>
/// <remarks>
/// Отдельная от <see cref="Rewards.TwitchMessagesPublisher"/>: publisher отвечает
/// за публикацию событий в RabbitMQ и фильтрацию, а рекордер нужен только
/// панели администратора. Так он не меняет поведение существующих обработчиков.
/// </remarks>
public sealed class TwitchInboxRecorder(
    ITwitchClient client,
    TwitchInboxStore store,
    ILogger<TwitchInboxRecorder> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.OnMessageReceived += OnMessageReceived;

        stoppingToken.Register(() => client.OnMessageReceived -= OnMessageReceived);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(object? sender, OnMessageReceivedArgs args)
    {
        try
        {
            var message = args.ChatMessage;

            store.Add(
                new TwitchInboxEntry(
                    DateTime.UtcNow,
                    message.Channel,
                    message.UserId,
                    message.Username,
                    message.DisplayName,
                    message.Message,
                    message.BotUsername,
                    message.UserDetail.IsModerator,
                    message.UserDetail.IsVip,
                    message.UserDetail.IsSubscriber,
                    message.UserId == TwitchConstants.ChannelId
                )
            );

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
    }
}
