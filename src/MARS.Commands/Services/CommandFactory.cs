using System;
using System.Collections.Generic;
using System.Linq;
using MARS.Commands.Services.Entitys.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MARS.Commands.Services;

public class CommandFactory(IServiceProvider serviceProvider, ILogger<CommandFactory> logger)
{
    public Dictionary<string, BaseCommand> CreateAllCommands()
    {
        var commands = new Dictionary<string, BaseCommand>(StringComparer.OrdinalIgnoreCase);

        var commandTypes = GetCommandTypes();

        foreach (var commandType in commandTypes)
        {
            try
            {
                var command = CreateCommand(commandType);
                if (command != null)
                {
                    commands[command.CommandName] = command;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Ошибка создания команды {CommandTypeName}: {ExMessage}",
                    commandType.Name,
                    ex.Message
                );
            }
        }

        return commands.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
    }

    public BaseCommand? CreateCommand(Type commandType)
    {
        if (!typeof(BaseCommand).IsAssignableFrom(commandType) || commandType.IsAbstract)
        {
            return null;
        }

        var constructors = commandType.GetConstructors();
        if (constructors.Length == 0)
        {
            return (BaseCommand)Activator.CreateInstance(commandType)!;
        }

        var bestConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();

        var parameters = bestConstructor.GetParameters();
        var resolvedParameters = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            try
            {
                resolvedParameters[i] = serviceProvider.GetRequiredService(parameter.ParameterType);
            }
            catch (InvalidOperationException)
            {
                if (parameter.HasDefaultValue)
                {
                    resolvedParameters[i] = parameter.DefaultValue!;
                }
                else
                {
                    logger.LogError(
                        "Не удалось разрешить зависимость {ParameterTypeName} для команды {CommandTypeName}",
                        parameter.ParameterType.Name,
                        commandType.Name
                    );
                    throw new InvalidOperationException(
                        $"Не удалось разрешить зависимость {parameter.ParameterType.Name} для команды {commandType.Name}"
                    );
                }
            }
        }

        return (BaseCommand)bestConstructor.Invoke(resolvedParameters);
    }

    private static IEnumerable<Type> GetCommandTypes()
    {
        var assembly = typeof(BaseCommand).Assembly;
        return assembly
            .GetTypes()
            .Where(t =>
                typeof(BaseCommand).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(BaseCommand)
            );
    }
}
