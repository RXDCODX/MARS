using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using Microsoft.Extensions.Hosting;

namespace MARS.Commands.Services;

public class CommandExecutorService(CommandFactory commandFactory)
    : BackgroundService,
        ICommandService
{
    private readonly Dictionary<string, BaseCommand> _commands = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    private void RegisterCommand(BaseCommand command)
    {
        _commands[command.CommandName] = command;

        foreach (var alias in command.Aliases)
        {
            _aliases[alias] = command.CommandName;
        }
    }

    public string[] GetUserCommands(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        string[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands
                    .Values.Where(c =>
                        !c.IsAdminCommand && c.IsVisibleIn(CommandVisibility.FullList)
                    )
                    .Select(c =>
                        isAddDescription ? $"{c.CommandName} - {c.Description}" : c.CommandName
                    ),
            ];
        }

        return result;
    }

    public string[] GetAdminCommands(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        string[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands
                    .Values.Where(c =>
                        c.IsAdminCommand && c.IsVisibleIn(CommandVisibility.FullList)
                    )
                    .Select(c =>
                        isAddDescription ? $"{c.CommandName} - {c.Description}" : c.CommandName
                    ),
            ];
        }

        return result;
    }

    public string[] GetUserCommands(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        string[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands
                    .Values.Where(c =>
                        !c.IsAdminCommand
                        && c.IsAvailableOnPlatform(platforms)
                        && c.IsVisibleIn(CommandVisibility.FullList)
                    )
                    .Select(c =>
                        isAddDescription ? $"{c.CommandName} - {c.Description}" : c.CommandName
                    ),
            ];
        }

        return result;
    }

    public string[] GetAdminCommands(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        string[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands
                    .Values.Where(c =>
                        c.IsAdminCommand
                        && c.IsAvailableOnPlatform(platforms)
                        && c.IsVisibleIn(CommandVisibility.FullList)
                    )
                    .Select(c =>
                        isAddDescription ? $"{c.CommandName} - {c.Description}" : c.CommandName
                    ),
            ];
        }

        return result;
    }

    public CommandParameterInfo[]? GetCommandParameters(
        string commandName,
        CancellationToken cancellationToken = default
    )
    {
        CommandParameterInfo[]? result = null;

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            if (_aliases.TryGetValue(commandName, out var actualCommandName))
            {
                commandName = actualCommandName;
            }

            if (_commands.TryGetValue(commandName, out var command))
            {
                result = command.GetParameterInfo();
            }
        }

        return result;
    }

    public BaseCommand[] GetUserCommandsInfo(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        BaseCommand[] result = [];

        if (_commands.Count > 0)
        {
            result = [.. _commands.Values.Where(c => !c.IsAdminCommand)];
        }

        return result;
    }

    public BaseCommand[] GetAdminCommandsInfo(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        BaseCommand[] result = [];

        if (_commands.Count > 0)
        {
            result = [.. _commands.Values.Where(c => c.IsAdminCommand)];
        }

        return result;
    }

    public BaseCommand[] GetUserCommandsInfo(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        BaseCommand[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands.Values.Where(c =>
                    !c.IsAdminCommand && c.IsAvailableOnPlatform(platforms)
                ),
            ];
        }

        return result;
    }

    public BaseCommand[] GetInlineCommandsInfo(
        Platform platforms,
        CancellationToken cancellationToken = default
    )
    {
        BaseCommand[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands.Values.Where(c =>
                    !c.IsAdminCommand
                    && c.IsAvailableOnPlatform(platforms)
                    && c.IsVisibleIn(CommandVisibility.Inline)
                    && (c.SupportsInline || c.SupportsMediaInline)
                ),
            ];
        }

        return result;
    }

    public BaseCommand[] GetAdminCommandsInfo(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    )
    {
        BaseCommand[] result = [];

        if (_commands.Count > 0)
        {
            result =
            [
                .. _commands.Values.Where(c =>
                    c.IsAdminCommand && c.IsAvailableOnPlatform(platforms)
                ),
            ];
        }

        return result;
    }

    public bool IsAdminCommand(string commandName, CancellationToken cancellationToken = default)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            if (_aliases.TryGetValue(commandName, out var actualCommandName))
            {
                commandName = actualCommandName;
            }

            if (_commands.TryGetValue(commandName, out var command))
            {
                result = command.IsAdminCommand;
            }
        }

        return result;
    }

    public bool IsCommandAvailable(string commandName, Platform platform)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            if (_aliases.TryGetValue(commandName, out var actualCommandName))
            {
                commandName = actualCommandName;
            }

            if (_commands.TryGetValue(commandName, out var command))
            {
                result = command.IsAvailableOnPlatform(platform);
            }
        }

        return result;
    }

    public async Task<string> ExecuteCommandAsync(
        string commandName,
        string input,
        Platform platform,
        CancellationToken cancellationToken = default
    )
    {
        var result =
            $"Команда '{commandName}' не найдена. Используйте /commands или /c для списка доступных команд.";

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            if (_aliases.TryGetValue(commandName, out var actualCommandName))
            {
                commandName = actualCommandName;
            }

            if (_commands.TryGetValue(commandName, out var command))
            {
                if (!command.IsAvailableOnPlatform(platform))
                {
                    result = $"Команда '{commandName}' недоступна на текущей платформе.";
                }
                else
                {
                    var commandInfo = command.GetParameterInfo();
                    var requiredParams = commandInfo.Where(p => p.Required).ToArray();
                    var inputParts = string.IsNullOrWhiteSpace(input)
                        ? []
                        : BaseCommand.ParseParametersWithQuotes(input);

                    if (inputParts.Length < requiredParams.Length)
                    {
                        var missingParam = requiredParams[inputParts.Length];
                        result =
                            $"Не хватает параметра '{missingParam.Name}'. Использование: {commandName} {string.Join(" ", requiredParams.Select(p => $"<{p.Name}>"))}";
                    }
                    else
                    {
                        var parameters = command.ParseParameters(input);

                        result = await ExecuteCommandAsync(
                            commandName,
                            parameters,
                            platform,
                            cancellationToken
                        );
                    }
                }
            }
        }

        return result;
    }

    public async Task<string> ExecuteCommandAsync(
        string commandName,
        Dictionary<string, object> parameters,
        Platform platform,
        CancellationToken cancellationToken = default
    )
    {
        var result =
            $"Команда '{commandName}' не найдена. Используйте /commands или /c для списка доступных команд.";

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            if (_aliases.TryGetValue(commandName, out var actualCommandName))
            {
                commandName = actualCommandName;
            }

            if (_commands.TryGetValue(commandName, out var command))
            {
                if (!command.IsAvailableOnPlatform(platform))
                {
                    result = $"Команда '{commandName}' недоступна на текущей платформе.";
                }
                else
                {
                    var commandInfo = command.GetParameterInfo();
                    var requiredParams = commandInfo.Where(p => p.Required).ToArray();

                    if (parameters.Count < requiredParams.Length)
                    {
                        var missingParam = requiredParams[parameters.Count];
                        result =
                            $"Не хватает параметра '{missingParam.Name}'. Использование: {commandName} {string.Join(" ", requiredParams.Select(p => $"<{p.Name}>"))}";
                    }
                    else
                    {
                        result = await command.ExecuteAsync(
                            parameters,
                            platform,
                            cancellationToken
                        );
                    }
                }
            }
        }

        return result;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var allCommands = commandFactory.CreateAllCommands();

        foreach (var command in allCommands)
        {
            RegisterCommand(command.Value);
        }

        return Task.FromResult(allCommands);
    }
}
