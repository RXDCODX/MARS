using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Clients.Http;

namespace MARS.Commands.Services.Entitys.Commands;

public class RollWaifuCommand(IWaifuGachaClient waifuGachaClient) : BaseCommand
{
    public override string CommandName => "rollwaifu";
    public override string Description => "Выполняет вайфу-ролл для пользователя";
    public override bool IsAdminCommand => true;
    public override Platform[] AvailablePlatforms =>
        [Platform.Telegram, Platform.Api, Platform.Twitch];

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "username",
                Description = "Имя пользователя",
                Type = CommandParameterType.String,
                Required = true,
            },
        ];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        if (!parameters.TryGetValue("username", out var usernameObj))
        {
            return CommandResult.Fail(
                "Необходимо указать имя пользователя",
                CommandErrorCode.BadArguments
            );
        }

        var username = (usernameObj as string ?? "").TrimStart('@');

        if (string.IsNullOrWhiteSpace(username))
        {
            return CommandResult.Fail(
                "Необходимо указать имя пользователя",
                CommandErrorCode.BadArguments
            );
        }

        var message = await waifuGachaClient.RollWaifuAsync(username, cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            return CommandResult.Fail(
                "Не удалось выполнить вайфу-ролл",
                CommandErrorCode.TargetUnreachable
            );
        }

        return CommandResult.Ok(message);
    }
}
