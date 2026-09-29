using System;
using System.Collections.Generic;
using System.Linq;
using MARS.Commands.Services.Entitys;

namespace MARS.Commands.Services.Adapters;

/// <summary>
/// Заглушка для Discord-адаптера команд.
/// Полная реализация зависит от MARS.Discord microservice.
/// </summary>
public class DiscordCommandService(ICommandService commandService)
    : PlatformCommandServiceBase<ulong>
{
    public override Platform Platform => Platform.Discord;

    protected override int DefaultMaxResponseLength => 1900;

    public override char[] CommandPrefixes => ['/', '!'];

    public override IEnumerable<string> UserCommands =>
        commandService.GetUserCommands(Platform.Discord);

    public override IEnumerable<string> AdminCommands =>
        commandService.GetAdminCommands(Platform.Discord);

    public override Func<ulong, bool> IsAdmin => _ => false;

    public bool IsCommandAvailable(string commandName)
    {
        return commandService.IsCommandAvailable(commandName, Platform.Discord);
    }
}
