using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.EventArgs;
using MARS.Discord.Application.Configuration;
using MARS.Discord.Infrastructure.Services.Gateway;
using MARS.Discord.Infrastructure.Services.PlayRequest;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wolverine;
using ServerDiscordConfiguration = MARS.Discord.Application.Configuration.DiscordConfiguration;

namespace MARS.Discord.Infrastructure.Services.CommandForwarder;

/// <summary>
/// Принимает сообщения Discord, детектит команды и публикует запрос на выполнение
/// в MARS.Commands. Админство определяется по конфигурации сервиса.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DiscordCommandForwarderService(
    IDiscordGatewayService gatewayService,
    IOptions<ServerDiscordConfiguration> options,
    DiscordPlayRequestService playRequestService,
    IServiceScopeFactory scopeFactory,
    ILogger<DiscordCommandForwarderService> logger
) : IHostedService
{
    private bool _registered;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_registered)
        {
            gatewayService.RegisterMessageCreatedHandler(HandleMessageCreatedAsync);
            _registered = true;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task HandleMessageCreatedAsync(DiscordClient client, MessageCreatedEventArgs args)
    {
        if (args.Author is null || args.Author.IsBot)
        {
            return;
        }

        var text = args.Message.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var prefixes = new[] { '/', '!' };
        if (!prefixes.Any(p => text.StartsWith(p)))
        {
            return;
        }

        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var commandName = parts[0].TrimStart(prefixes).Trim();
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return;
        }

        var input = parts.Length > 1 ? parts[1] : string.Empty;
        if (commandName.Equals("play", StringComparison.OrdinalIgnoreCase))
        {
            await playRequestService.TryHandleMessageAsync(client, args, CancellationToken.None);
            return;
        }

        var isAdmin = options.Value.AdminIdsArray.Contains(args.Author.Id);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new CommandExecuteRequestedEvent
                {
                    Platform = CommandPlatform.Discord,
                    CommandName = commandName,
                    Input = input,
                    IsAdmin = isAdmin,
                    IsModerator = false,
                    IsVip = false,
                    IsBroadcaster = false,
                    UserId = args.Author.Id.ToString(),
                    UserName = args.Author.Username ?? string.Empty,
                    DisplayName = args.Author.Username ?? string.Empty,
                    ReplyTo = args.Channel.Id.ToString(),
                    SourceMessageId = args.Message.Id.ToString(),
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось опубликовать запрос команды '{Command}'", commandName);
        }
    }
}
