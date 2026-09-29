using System;
using System.Collections.Generic;
using System.Linq;
using MARS.Commands.Services.Entitys;

namespace MARS.Commands.Services.Adapters;

/// <summary>
/// Заглушка для Twitch-адаптера команд.
/// Полная реализация зависит от MARS.TwitchCore microservice.
/// </summary>
public class TwitchCommandService(ICommandService commandService)
    : PlatformCommandServiceBase<string>
{
    public override Platform Platform => Platform.Twitch;

    protected override int DefaultMaxResponseLength => 500;

    public override char[] CommandPrefixes => ['!'];

    public override IEnumerable<string> UserCommands =>
        commandService
            .GetUserCommands(Platform.Twitch, false, CancellationToken.None)
            .Select(c => $"!{c}");

    public override IEnumerable<string> AdminCommands =>
        commandService
            .GetAdminCommands(Platform.Twitch, false, CancellationToken.None)
            .Select(c => $"!{c}");

    public override Func<string, bool> IsAdmin => _ => false;

    public bool IsCommandAvailable(string commandName)
    {
        return commandService.IsCommandAvailable(commandName, Platform.Twitch);
    }

    public override string ValidateResponse(string response) => response;
}
