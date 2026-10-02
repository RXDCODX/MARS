using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Победы вызывающего пользователя в мини-играх. Таблица лежит в
/// MARS.TwitchCore, поэтому команда ходит туда по внутреннему API.
/// </summary>
public class MyWinsCommand(ILeaderboardClient leaderboardClient) : BaseCommand
{
    public override string CommandName => "mywins";
    public override string Description => "Показывает ваши победы в мини-играх";
    public override bool IsAdminCommand => false;
    public override Platform[] AvailablePlatforms => [Platform.Twitch];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var twitchId = TwitchCaller.ResolveId(parameters);

        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return CommandResult.Fail(
                "Не удалось определить вашего пользователя.",
                CommandErrorCode.BadArguments
            );
        }

        var stats = await leaderboardClient.GetUserStatsAsync(twitchId, cancellationToken);

        if (stats is null)
        {
            return CommandResult.Fail(
                "Не удалось получить статистику: сервис недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        if (stats.User is null)
        {
            return CommandResult.Ok("У вас пока нет побед в мини-играх.");
        }

        var name = stats.User.DisplayName ?? stats.User.TwitchId;

        return CommandResult.Ok(
            $"@{name}, вы на {stats.Place} месте! "
                + $"Побед: {stats.User.TotalWins} "
                + $"(рулетка: {stats.User.RussianRouletteWins}, "
                + $"викторина: {stats.User.TriviaWins})."
        );
    }
}
