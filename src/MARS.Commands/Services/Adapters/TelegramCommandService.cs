using System;
using System.Collections.Generic;
using System.Linq;
using MARS.Commands.Services.Entitys;

namespace MARS.Commands.Services.Adapters;

/// <summary>
/// Заглушка для Telegram-адаптера команд.
/// Полная реализация зависит от MARS.Telegram microservice.
/// </summary>
public class TelegramCommandService(ICommandService commandService)
    : PlatformCommandServiceBase<long>
{
    public override Entitys.Platform Platform => Entitys.Platform.Telegram;

    protected override int DefaultMaxResponseLength => 4096;

    public override char[] CommandPrefixes => ['/'];

    public override IEnumerable<string> UserCommands =>
        commandService.GetUserCommands(Entitys.Platform.Telegram);

    public override IEnumerable<string> AdminCommands =>
        commandService.GetAdminCommands(Entitys.Platform.Telegram);

    public override Func<long, bool> IsAdmin => _ => false;

    public bool IsCommandAvailable(string commandName)
    {
        return commandService.IsCommandAvailable(commandName, Entitys.Platform.Telegram);
    }
}
