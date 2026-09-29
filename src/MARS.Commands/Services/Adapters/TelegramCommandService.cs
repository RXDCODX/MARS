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
    public override Platform Platform => Platform.Telegram;

    protected override int DefaultMaxResponseLength => 4096;

    public override char[] CommandPrefixes => ['/'];

    public override IEnumerable<string> UserCommands =>
        commandService.GetUserCommands(Platform.Telegram);

    public override IEnumerable<string> AdminCommands =>
        commandService.GetAdminCommands(Platform.Telegram);

    public override Func<long, bool> IsAdmin => _ => false;

    public bool IsCommandAvailable(string commandName)
    {
        return commandService.IsCommandAvailable(commandName, Platform.Telegram);
    }
}
