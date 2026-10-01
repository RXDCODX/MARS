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
                result = IsAdminOnly(command);
            }
        }

        return result;
    }

    /// <summary>
    /// Единственное место, решающее, что команда закрыта правами администратора.
    /// Вызывается и из <see cref="IsAdminCommand"/>, и из гейта в
    /// <c>ExecuteCommandAsync</c>: если бы правило жило в двух местах, то
    /// усложнение вроде whitelist'а в одном из них тихо обошло бы другое.
    /// </summary>
    private static bool IsAdminOnly(BaseCommand command) => command.IsAdminCommand;

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

    public async Task<CommandResult> ExecuteCommandAsync(
        string commandName,
        string input,
        Platform platform,
        bool isAdmin = false,
        CancellationToken cancellationToken = default
    )
    {
        var result = CommandResult.Fail(
            $"Команда '{commandName}' не найдена. Используйте /commands или /c для списка доступных команд.",
            CommandErrorCode.UnknownCommand
        );

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
                    result = CommandResult.Fail(
                        $"Команда '{commandName}' недоступна на текущей платформе.",
                        CommandErrorCode.NotAvailableOnPlatform
                    );
                }
                else if (IsAdminOnly(command) && !isAdmin)
                {
                    // Позиция гейта — как в монолите: после резолва алиаса и
                    // проверки платформы, до разбора параметров и исполнения.
                    // Алиас обязан быть разрешён к этому моменту, иначе
                    // «adhdstart» прошёл бы как пользовательская команда.
                    result = CommandResult.Fail(
                        $"Команда '{commandName}' доступна только администраторам.",
                        CommandErrorCode.NotAllowed
                    );
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
                        result = CommandResult.Fail(
                            $"Не хватает параметра '{missingParam.Name}'. Использование: {commandName} {string.Join(" ", requiredParams.Select(p => $"<{p.Name}>"))}",
                            CommandErrorCode.BadArguments
                        );
                    }
                    else
                    {
                        var parameters = command.ParseParameters(input);

                        result = await ExecuteCommandAsync(
                            commandName,
                            parameters,
                            platform,
                            isAdmin,
                            cancellationToken
                        );
                    }
                }
            }
        }

        return result;
    }

    public async Task<CommandResult> ExecuteCommandAsync(
        string commandName,
        Dictionary<string, object> parameters,
        Platform platform,
        bool isAdmin = false,
        CancellationToken cancellationToken = default
    )
    {
        var result = CommandResult.Fail(
            $"Команда '{commandName}' не найдена. Используйте /commands или /c для списка доступных команд.",
            CommandErrorCode.UnknownCommand
        );

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
                    result = CommandResult.Fail(
                        $"Команда '{commandName}' недоступна на текущей платформе.",
                        CommandErrorCode.NotAvailableOnPlatform
                    );
                }
                else if (IsAdminOnly(command) && !isAdmin)
                {
                    // Повторная проверка: перегрузка со словарём параметров
                    // вызывается напрямую (gRPC-сервис, тесты) и не проходит
                    // через разбор строки ввода, где гейт стоит выше.
                    result = CommandResult.Fail(
                        $"Команда '{commandName}' доступна только администраторам.",
                        CommandErrorCode.NotAllowed
                    );
                }
                else
                {
                    var commandInfo = command.GetParameterInfo();
                    var requiredParams = commandInfo.Where(p => p.Required).ToArray();

                    if (parameters.Count < requiredParams.Length)
                    {
                        var missingParam = requiredParams[parameters.Count];
                        result = CommandResult.Fail(
                            $"Не хватает параметра '{missingParam.Name}'. Использование: {commandName} {string.Join(" ", requiredParams.Select(p => $"<{p.Name}>"))}",
                            CommandErrorCode.BadArguments
                        );
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

    /// <summary>
    /// Наполняет реестр команд. Вызывается из <see cref="StartAsync"/> до
    /// <c>base.StartAsync</c>: <c>BackgroundService</c> запускает
    /// <c>ExecuteAsync</c> сам, но тот возвращает уже завершённую задачу, и хост
    /// стартует с пустым реестром — ни одна команда не находится.
    /// </summary>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        RegisterAllCommands();

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    private void RegisterAllCommands()
    {
        var allCommands = commandFactory.CreateAllCommands();

        foreach (var command in allCommands)
        {
            RegisterCommand(command.Value);
        }
    }
}
