using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services.Commands;
using MARS.Twitch.Infrastructure.Services.Validation;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using Wolverine;

namespace MARS.Twitch.Infrastructure.Services.Commands;

/// <summary>
/// Принимает сообщения Twitch-чата, детектит команды (префикс «!») и публикует
/// запрос на выполнение в MARS.Commands. Админство определяется сервисом платформы.
/// </summary>
public sealed class TwitchCommandForwarderService(
    ITwitchClient client,
    IHostApplicationLifetime lifetime,
    ITwitchEventValidationService validator,
    IServiceScopeFactory scopeFactory,
    ILogger<TwitchCommandForwarderService> logger
) : BackgroundService
{
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

        return Task.CompletedTask;
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

        var message = args.ChatMessage.Message;
        if (string.IsNullOrWhiteSpace(message) || !message.StartsWith('!'))
        {
            return;
        }

        var parts = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var commandName = parts[0].TrimStart('!').Trim();
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return;
        }

        var input = parts.Length > 1 ? parts[1] : string.Empty;
        var isAdmin = TwitchCommandPermissions.IsAdmin(
            args.ChatMessage.UserId,
            args.ChatMessage.UserDetail.IsModerator
        );

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new CommandExecuteRequestedEvent
                {
                    Platform = CommandPlatform.Twitch,
                    CommandName = commandName,
                    Input = input,
                    IsAdmin = isAdmin,
                    IsModerator = args.ChatMessage.UserDetail.IsModerator,
                    IsBroadcaster = TwitchCommandPermissions.IsBroadcaster(args.ChatMessage.UserId),
                    IsVip = false,
                    UserId = args.ChatMessage.UserId,
                    UserName = args.ChatMessage.Username,
                    DisplayName = args.ChatMessage.DisplayName,
                    ReplyTo = args.ChatMessage.Channel,
                    SourceMessageId = args.ChatMessage.Id,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось опубликовать запрос команды '{Command}'", commandName);
        }
    }
}
