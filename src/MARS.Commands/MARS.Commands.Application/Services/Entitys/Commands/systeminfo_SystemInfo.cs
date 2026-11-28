using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Application.Services.Entitys.Commands;

[ExcludeFromCodeCoverage]
public class SysteminfoCommand : BaseCommand
{
    public override string CommandName => "systeminfo";

    public override string Description => "Показывает информацию о среде выполнения";

    public override bool IsAdminCommand => true;

    public override Platform[] AvailablePlatforms => [Platform.Telegram, Platform.Api];

    public override string[] Aliases => ["sysinfo"];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var text =
            $"Время: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\n"
            + $".NET: {Environment.Version}\n"
            + $"OS: {Environment.OSVersion}\n"
            + $"CPU: {Environment.ProcessorCount}\n"
            + $"Память GC: {GC.GetTotalMemory(false) / 1024 / 1024} MB\n"
            + $"Среда: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}";

        return Task.FromResult(CommandResult.Ok(text));
    }
}