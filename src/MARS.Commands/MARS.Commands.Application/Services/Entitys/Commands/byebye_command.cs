using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;

namespace MARS.Commands.Application.Services.Entitys.Commands;

public class ByebyeCommand : BaseCommand
{
    public override string CommandName => "byebye";
    public override string Description => "Прощание с пользователем";
    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Discord, Platform.Vk, Platform.Twitch];

    public override CommandVisibility Visibility => CommandVisibility.All; // Видна везде

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        const string usage = """
            Пока-пока! 👋
            Надеюсь, ты скоро вернешься! 😊
            """;

        return Task.FromResult(CommandResult.Ok(usage));
    }
}
