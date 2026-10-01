using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using Microsoft.Extensions.Logging;

namespace MARS.Commands.Services.Adapters;

public class ApiCommandService(ICommandService commandService, ILogger<ApiCommandService> logger)
    : PlatformCommandServiceBase<string>
{
    public override Platform Platform => Platform.Api;

    protected override int DefaultMaxResponseLength => 10000;

    public override char[] CommandPrefixes => ['/', '!'];

    public override IEnumerable<string> UserCommands =>
        commandService.GetUserCommands(Platform.Api);

    public override IEnumerable<string> AdminCommands =>
        commandService.GetAdminCommands(Platform.Api);

    /// <summary>
    /// Через HTTP-маршрут админ-команд не выдать: вызывающий — шлюз или другой
    /// сервис, а не оператор стенда, и проверить его права здесь нечем.
    /// Админ-команды живут только на платформах, где права вызывающего известны
    /// заранее (Twitch, Telegram, Discord).
    /// </summary>
    public override Func<string, bool> IsAdmin => _ => false;

    public async Task<string> ExecuteCommandAsync(
        string commandName,
        string input,
        CancellationToken cancellationToken = default
    )
    {
        var result =
            $"Команда '{commandName}' не найдена. Используйте /commands для списка доступных команд.";

        if (!string.IsNullOrWhiteSpace(commandName))
        {
            try
            {
                result = await commandService.ExecuteCommandAsync(
                    commandName,
                    input,
                    Platform.Api,
                    // Через HTTP вызывающий — шлюз или соседний сервис, а не
                    // оператор стенда, поэтому админ-команды отсюда недостижимы.
                    isAdmin: false,
                    cancellationToken: cancellationToken
                );

                result = ValidateResponse(result);

                logger.LogInformation(
                    "Команда '{CommandName}' выполнена через API с результатом: {Result}",
                    commandName,
                    result.Length > 100 ? string.Concat(result.AsSpan(0, 100), "...") : result
                );
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Ошибка параметров для команды '{CommandName}'", commandName);
                result = $"Ошибка параметров: {ex.Message}";
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Ошибка при выполнении команды '{CommandName}' через API",
                    commandName
                );
                result = $"Ошибка при выполнении команды '{commandName}': {ex.Message}";
            }
        }

        return result;
    }

    public bool IsAdminCommand(string commandName)
    {
        return commandService.IsAdminCommand(commandName);
    }

    public virtual bool IsCommandAvailable(string commandName)
    {
        return commandService.IsCommandAvailable(commandName, Platform.Api);
    }

    public override string ValidateResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return response;
        }

        var maxLength = GetMaxResponseLength();

        if (response.Length <= maxLength)
        {
            return response;
        }

        var truncated = response.Substring(0, maxLength - 10);
        return truncated + "\n\n[Ответ обрезан...]";
    }

    public string[] GetUserCommands(Platform platforms)
    {
        return commandService.GetUserCommands(platforms);
    }

    public string[] GetAdminCommands(Platform platforms)
    {
        return commandService.GetAdminCommands(platforms);
    }

    public CommandParameterInfo[]? GetCommandParameters(string commandName)
    {
        return commandService.GetCommandParameters(commandName);
    }

    public BaseCommand[] GetUserCommandsInfo(Platform platform)
    {
        return commandService
            .GetUserCommandsInfo()
            .Where(e => e.AvailablePlatforms.Contains(Platform.Api))
            .ToArray();
    }

    public BaseCommand[] GetAdminCommandsInfo(Platform platform)
    {
        return commandService
            .GetAdminCommandsInfo()
            .Where(e => e.AvailablePlatforms.Contains(Platform.Api))
            .ToArray();
    }
}
