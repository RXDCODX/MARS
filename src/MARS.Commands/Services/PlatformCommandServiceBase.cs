using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARS.Commands.Services.Entitys;

namespace MARS.Commands.Services;

public abstract class PlatformCommandServiceBase<T>
    where T : notnull
{
    public abstract Platform Platform { get; }

    protected virtual int DefaultMaxResponseLength => 1000;

    public virtual char[] CommandPrefixes => ['/'];

    public abstract IEnumerable<string> UserCommands { get; }
    public abstract IEnumerable<string> AdminCommands { get; }

    public abstract Func<T, bool> IsAdmin { get; }

    public virtual bool IsUserAdmin(T userId)
    {
        return IsAdmin.Invoke(userId);
    }

    public virtual char[] GetCommandPrefixes()
    {
        return CommandPrefixes;
    }

    public virtual string TrimCommandPrefix(string commandText)
    {
        var result = commandText;

        if (!string.IsNullOrWhiteSpace(commandText))
        {
            result = commandText.TrimStart(CommandPrefixes);
        }

        return result;
    }

    public virtual bool StartsWithCommandPrefix(string text)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(text))
        {
            result = CommandPrefixes.Any(prefix => text.StartsWith(prefix));
        }

        return result;
    }

    public virtual string GetCommandsList(T userId, bool includeAdminCommands = false)
    {
        var isAdmin = IsUserAdmin(userId);

        if (includeAdminCommands && !isAdmin)
        {
            return "У вас нет прав для просмотра админских команд.";
        }

        var commands = new List<string>();

        commands.AddRange(UserCommands);

        if (includeAdminCommands && isAdmin)
        {
            commands.AddRange(AdminCommands);
        }

        if (commands.Count == 0)
        {
            return "Нет доступных команд для вашей роли.";
        }

        var result =
            includeAdminCommands && isAdmin
                ? "Доступные команды (включая админские):\n"
                : "Доступные команды:\n";

        result += string.Join("\n", commands);

        return result;
    }

    public virtual string GetCommandsList(
        T userId,
        IEnumerable<string> userCommands,
        IEnumerable<string> adminCommands,
        bool includeAdminCommands = false
    )
    {
        var isAdmin = IsUserAdmin(userId);

        if (includeAdminCommands && !isAdmin)
        {
            return "У вас нет прав для просмотра админских команд.";
        }

        var commands = new List<string>();

        commands.AddRange(userCommands);

        if (includeAdminCommands && isAdmin)
        {
            commands.AddRange(adminCommands);
        }

        if (commands.Count == 0)
        {
            return "Нет доступных команд для вашей роли.";
        }

        StringBuilder result = new(
            includeAdminCommands && isAdmin
                ? "Доступные команды (включая админские): "
                    + Environment.NewLine
                    + Environment.NewLine
                : "Доступные команды: " + Environment.NewLine + Environment.NewLine
        );

        result.AppendJoin(Environment.NewLine + Environment.NewLine, commands.Order());

        return result.ToString();
    }

    public virtual string ValidateResponse(string response)
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

        var truncated = response.Substring(0, maxLength - 3);
        return truncated + "...";
    }

    public virtual int GetMaxResponseLength()
    {
        return DefaultMaxResponseLength;
    }
}
