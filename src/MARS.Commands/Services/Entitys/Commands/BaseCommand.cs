using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MARS.Commands.Services.Entitys.Commands;

public abstract class BaseCommand
{
    public abstract string CommandName { get; }
    public abstract string Description { get; }
    public abstract bool IsAdminCommand { get; }

    public virtual Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Discord, Platform.Vk, Platform.Twitch];

    public virtual string[] Aliases => [];

    public virtual CommandParameterInfo[] Parameters => [];

    public virtual CommandVisibility Visibility => CommandVisibility.All;

    public virtual bool SupportsInline => false;

    public virtual bool SupportsMediaInline => false;

    public virtual string? InlinePreviewUrl => null;

    public virtual string InlineTitle => CommandName;

    public virtual string InlineDescription => Description;

    public abstract Task<string> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    );

    public virtual Dictionary<string, object> ParseParameters(string input)
    {
        var parameters = new Dictionary<string, object>();
        var commandParameters = Parameters;

        if (string.IsNullOrWhiteSpace(input))
        {
            foreach (
                var param in commandParameters.Where(p =>
                    p is { Required: false, DefaultValue: not null }
                )
            )
            {
                parameters[param.Name] = ConvertValue(param.DefaultValue!, param.Type);
            }
            return parameters;
        }

        var parts = ParseParametersWithQuotes(input);
        var currentIndex = 0;

        foreach (var param in commandParameters)
        {
            if (currentIndex >= parts.Length)
            {
                if (param.Required)
                {
                    throw new ArgumentException($"Обязательный параметр '{param.Name}' не указан");
                }
                if (param.DefaultValue != null)
                {
                    parameters[param.Name] = ConvertValue(param.DefaultValue, param.Type);
                }
                continue;
            }

            if (
                param.Type == "string"
                && param == commandParameters.Last()
                && currentIndex < parts.Length - 1
            )
            {
                var remainingParts = parts.Skip(currentIndex);
                parameters[param.Name] = string.Join(" ", remainingParts);
                break;
            }

            parameters[param.Name] = ConvertValue(parts[currentIndex], param.Type);
            currentIndex++;
        }

        return parameters;
    }

    public static string[] ParseParametersWithQuotes(string input)
    {
        var result = new List<string>();
        var currentPart = new System.Text.StringBuilder();
        var inQuotes = false;
        char? quoteChar = null;
        var i = 0;

        while (i < input.Length)
        {
            var currentChar = input[i];

            if (!inQuotes)
            {
                if (currentChar == '"' || currentChar == '\'')
                {
                    inQuotes = true;
                    quoteChar = currentChar;
                }
                else if (char.IsWhiteSpace(currentChar))
                {
                    if (currentPart.Length > 0)
                    {
                        result.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                }
                else
                {
                    currentPart.Append(currentChar);
                }
            }
            else
            {
                if (currentChar == quoteChar)
                {
                    if (i + 1 < input.Length && input[i + 1] == quoteChar)
                    {
                        currentPart.Append(quoteChar);
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        quoteChar = null;
                        result.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                }
                else if (currentChar == '\\' && i + 1 < input.Length)
                {
                    var nextChar = input[i + 1];
                    if (nextChar == '\\' || nextChar == '"' || nextChar == '\'')
                    {
                        currentPart.Append(nextChar);
                        i++;
                    }
                    else
                    {
                        currentPart.Append(currentChar);
                    }
                }
                else
                {
                    currentPart.Append(currentChar);
                }
            }

            i++;
        }

        if (currentPart.Length > 0)
        {
            result.Add(currentPart.ToString());
        }

        return result.ToArray();
    }

    public virtual CommandParameterInfo[] GetParameterInfo()
    {
        return Parameters;
    }

    public virtual Platform[] GetAvailablePlatforms()
    {
        return AvailablePlatforms;
    }

    public virtual bool IsAvailableOnPlatform(Platform platform)
    {
        var availablePlatforms = GetAvailablePlatforms();
        return Enumerable.Contains(availablePlatforms, platform);
    }

    public virtual bool IsVisibleIn(CommandVisibility visibility)
    {
        return (Visibility & visibility) != 0;
    }

    private static object ConvertValue(string value, string type)
    {
        return type.ToLower() switch
        {
            "int" => int.Parse(value),
            "long" => long.Parse(value),
            "double" => double.Parse(value),
            "bool" => value.Equals("true", StringComparison.OrdinalIgnoreCase),
            "string" => value,
            _ => value,
        };
    }
}
