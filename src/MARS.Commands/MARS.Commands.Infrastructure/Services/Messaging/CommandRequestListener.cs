using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Twitch.Control;
using MARS.Shared.Kernel.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Infrastructure.Services.Messaging;

using Entitys = MARS.Commands.Application.Services.Entitys;

/// <summary>
/// Потребитель запросов на выполнение команд от сервисов платформ.
/// Выполняет команду через <see cref="ICommandService"/> и доставляет ответ
/// обратно на платформу по gRPC (outbox).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CommandRequestListener(
    ICommandService commandService,
    IServiceScopeFactory scopeFactory,
    IMarsGrpcClient grpcClient,
    IBusinessActivityLogger businessActivity,
    ILogger<CommandRequestListener> logger
) : MarsEventListener<CommandExecuteRequestedEvent>
{
    private static readonly char[] TwitchPrefixes = ['!'];
    private static readonly char[] DiscordPrefixes = ['/', '!'];
    private static readonly char[] TelegramPrefixes = ['/'];

    public override async Task Handle(
        CommandExecuteRequestedEvent message,
        CancellationToken cancellationToken
    )
    {
        if (message is null || string.IsNullOrWhiteSpace(message.CommandName))
        {
            return;
        }

        var platform = MapPlatform(message.Platform);
        var prefix = PrefixFor(platform);

        var commandName = message.CommandName.Trim().TrimStart(prefix);
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return;
        }

        businessActivity.Log(
            new BusinessActivity
            {
                Category = "command",
                Action = "command-requested",
                Outcome = BusinessActivityOutcome.Accepted,
                ActorType = "user",
                ActorId = message.UserId,
                ActorDisplayName = message.DisplayName,
                Platform = message.Platform.ToString(),
                SubjectType = "command",
                SubjectId = commandName,
                CorrelationId = message.RequestId,
                Summary = $"вызвал команду {prefix}{commandName}",
            }
        );

        // Спец-команды «commands» / «c»: собираем список с учётом прав вызывающего.
        if (commandName.Equals("commands", StringComparison.OrdinalIgnoreCase))
        {
            var text = BuildCommandsList(platform, message.IsAdmin, full: true);
            await DeliverAsync(message, text, cancellationToken);
            return;
        }

        if (commandName.Equals("c", StringComparison.OrdinalIgnoreCase))
        {
            var text = BuildCommandsList(platform, message.IsAdmin, full: false);
            await DeliverAsync(message, text, cancellationToken);
            return;
        }

        if (!commandService.IsCommandAvailable(commandName, platform))
        {
            await DeliverAsync(
                message,
                $"Команда '{commandName}' недоступна на этой платформе.",
                cancellationToken
            );
            return;
        }

        var commandInfo = commandService.GetCommandParameters(commandName);
        var requiredParams = (commandInfo ?? Array.Empty<CommandParameterInfo>())
            .Where(p => p.Required)
            .ToArray();

        var inputParts = string.IsNullOrWhiteSpace(message.Input)
            ? Array.Empty<string>()
            : BaseCommand.ParseParametersWithQuotes(message.Input);

        if (inputParts.Length < requiredParams.Length)
        {
            var missing = requiredParams[inputParts.Length];
            await DeliverAsync(
                message,
                $"Не хватает параметра '{missing.Name}'. Использование: {prefix}{commandName} {string.Join(" ", requiredParams.Select(p => $"<{p.Name}>"))}",
                cancellationToken
            );
            return;
        }

        if (commandService.IsAdminCommand(commandName) && !message.IsAdmin)
        {
            await DeliverAsync(
                message,
                $"Команда '{commandName}' доступна только администраторам.",
                cancellationToken
            );
            return;
        }

        if (commandService.IsDurableCommand(commandName))
        {
            try
            {
                var durableParameters = commandService.ParseParameters(commandName, message.Input);
                durableParameters["userId"] = message.UserId;
                durableParameters["username"] = message.UserName;
                durableParameters["displayName"] = message.DisplayName;
                durableParameters["platform"] = platform;
                durableParameters["isModerator"] = message.IsModerator;
                durableParameters["isVip"] = message.IsVip;
                durableParameters["isBroadcaster"] = message.IsBroadcaster;

                using var scope = scopeFactory.CreateScope();
                var ledger = scope.ServiceProvider.GetRequiredService<ICommandExecutionLedger>();
                var invocation = new CommandInvocation(
                    message.RequestId,
                    message.Platform.ToString(),
                    commandName,
                    message.Input ?? string.Empty,
                    message.UserId ?? string.Empty,
                    message.DisplayName,
                    message.IsAdmin,
                    !string.IsNullOrWhiteSpace(message.RequestId),
                    message.ReplyTo,
                    message.SourceMessageId,
                    message.InlineMessageId
                );

                await ledger.AcceptAsync(invocation, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to accept durable command '{CommandName}'",
                    commandName
                );
                await DeliverAsync(
                    message,
                    $"Ошибка при выполнении команды '{commandName}': {ex.Message}",
                    cancellationToken
                );
            }

            return;
        }

        try
        {
            var parameters = commandService.ParseParameters(commandName, message.Input);
            parameters["userId"] = message.UserId;
            parameters["username"] = message.UserName;
            parameters["displayName"] = message.DisplayName;
            parameters["platform"] = platform;
            parameters["isModerator"] = message.IsModerator;
            parameters["isVip"] = message.IsVip;
            parameters["isBroadcaster"] = message.IsBroadcaster;

            var result = await commandService.ExecuteCommandAsync(
                commandName,
                parameters,
                platform,
                message.IsAdmin,
                cancellationToken
            );

            var text = string.IsNullOrWhiteSpace(result.Text)
                ? string.Empty
                : ValidateResponse(result.Text, platform);

            if (!string.IsNullOrWhiteSpace(text))
            {
                await DeliverAsync(message, text, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при выполнении команды '{CommandName}'", commandName);
            businessActivity.Log(
                new BusinessActivity
                {
                    Category = "command",
                    Action = "command-failed",
                    Outcome = BusinessActivityOutcome.Failed,
                    ActorType = "user",
                    ActorId = message.UserId,
                    ActorDisplayName = message.DisplayName,
                    Platform = message.Platform.ToString(),
                    SubjectType = "command",
                    SubjectId = commandName,
                    CorrelationId = message.RequestId,
                    Summary = $"команда {prefix}{commandName} завершилась ошибкой: {ex.Message}",
                    Level = BusinessActivityLevel.Error,
                },
                ex
            );
            await DeliverAsync(
                message,
                $"Ошибка при выполнении команды '{commandName}': {ex.Message}",
                cancellationToken
            );
        }
    }

    private static Entitys.Platform MapPlatform(CommandPlatform platform) =>
        platform switch
        {
            CommandPlatform.Telegram => Entitys.Platform.Telegram,
            CommandPlatform.Twitch => Entitys.Platform.Twitch,
            CommandPlatform.Discord => Entitys.Platform.Discord,
            _ => Entitys.Platform.Api,
        };

    private static char PrefixFor(Entitys.Platform platform) =>
        platform switch
        {
            Entitys.Platform.Twitch => '!',
            Entitys.Platform.Discord => '/',
            Entitys.Platform.Telegram => '/',
            _ => '/',
        };

    private string BuildCommandsList(Entitys.Platform platform, bool isAdmin, bool full)
    {
        var userCommands = commandService
            .GetUserCommandsInfo(platform)
            .Where(c =>
                c.IsVisibleIn(full ? CommandVisibility.FullList : CommandVisibility.ShortList)
            )
            .Select(c => $"{PrefixFor(platform)}{c.CommandName}")
            .ToList();

        var adminCommands = isAdmin
            ? commandService
                .GetAdminCommandsInfo(platform)
                .Where(c =>
                    c.IsVisibleIn(full ? CommandVisibility.FullList : CommandVisibility.ShortList)
                )
                .Select(c => $"{PrefixFor(platform)}{c.CommandName}")
                .ToList()
            : new List<string>();

        var all = userCommands.Concat(adminCommands).OrderBy(c => c).ToList();
        if (all.Count == 0)
        {
            return "Нет доступных команд для вашей роли.";
        }

        var prefix = isAdmin ? "Доступные команды (включая админские):" : "Доступные команды:";
        return $"{prefix}\n{string.Join("\n", all)}";
    }

    private static string ValidateResponse(string response, Entitys.Platform platform)
    {
        var maxLength = platform switch
        {
            Entitys.Platform.Twitch => 500,
            Entitys.Platform.Discord => 1900,
            Entitys.Platform.Telegram => 4096,
            _ => 10000,
        };

        if (response.Length <= maxLength)
        {
            return response;
        }

        var truncated = response[..(maxLength - 10)];
        return truncated + "\n\n[Ответ обрезан...]";
    }

    private async Task DeliverAsync(
        CommandExecuteRequestedEvent message,
        string text,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            switch (message.Platform)
            {
                case CommandPlatform.Twitch:
                    await grpcClient.Twitch.SendChatMessageAsync(
                        new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
                        {
                            Channel = message.ReplyTo,
                            Message = text,
                            ReplyTo = message.SourceMessageId ?? string.Empty,
                        },
                        cancellationToken
                    );
                    break;

                case CommandPlatform.Discord:
                    if (ulong.TryParse(message.ReplyTo, out var channelId))
                    {
                        await grpcClient.Discord.SendMessageAsync(
                            channelId.ToString(),
                            text,
                            cancellationToken
                        );
                    }
                    break;

                case CommandPlatform.Telegram:
                    if (long.TryParse(message.ReplyTo, out var chatId))
                    {
                        await grpcClient.Telegram.Messages.SendAsync(
                            chatId,
                            text,
                            cancellationToken
                        );
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Не удалось доставить ответ на платформу {Platform}",
                message.Platform
            );
        }
    }
}
