using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services;

public interface ICommandService
{
    string[] GetUserCommands(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    string[] GetAdminCommands(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    string[] GetUserCommands(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    string[] GetAdminCommands(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    CommandParameterInfo[]? GetCommandParameters(
        string commandName,
        CancellationToken cancellationToken = default
    );

    BaseCommand[] GetUserCommandsInfo(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    BaseCommand[] GetAdminCommandsInfo(
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    BaseCommand[] GetUserCommandsInfo(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    BaseCommand[] GetInlineCommandsInfo(
        Platform platforms,
        CancellationToken cancellationToken = default
    );

    BaseCommand[] GetAdminCommandsInfo(
        Platform platforms,
        bool isAddDescription = true,
        CancellationToken cancellationToken = default
    );

    bool IsAdminCommand(string commandName, CancellationToken cancellationToken = default);

    bool IsCommandAvailable(string commandName, Platform platform);

    Dictionary<string, object> ParseParameters(string input, CommandParameterInfo[]? commandInfo)
    {
        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var inputParts = string.IsNullOrWhiteSpace(input)
            ? Array.Empty<string>()
            : BaseCommand.ParseParametersWithQuotes(input);

        if (commandInfo is not null && commandInfo.Length > 0)
        {
            var lastParam = commandInfo[^1];
            for (var i = 0; i < commandInfo.Length; i++)
            {
                var p = commandInfo[i];
                if (i < inputParts.Length)
                {
                    if (p == lastParam && p.Type == "string" && i < inputParts.Length - 1)
                    {
                        parameters[p.Name] = string.Join(" ", inputParts.Skip(i));
                        break;
                    }

                    parameters[p.Name] = inputParts[i];
                }
            }
        }

        return parameters;
    }

    Task<string> ExecuteCommandAsync(
        string commandName,
        string input,
        Platform platform,
        CancellationToken cancellationToken = default
    );

    Task<string> ExecuteCommandAsync(
        string commandName,
        Dictionary<string, object> parameters,
        Platform platform,
        CancellationToken cancellationToken = default
    );
}
