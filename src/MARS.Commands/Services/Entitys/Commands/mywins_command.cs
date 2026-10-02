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
        var twitchId = ResolveUserId(parameters);

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

    /// <summary>
    /// Twitch-адаптер передаёт объект <c>TwitchUser</c>, API и Telegram — строку.
    /// Оба пути принимаются, иначе команда работала бы только на одной
    /// платформе.
    /// </summary>
    private static string? ResolveUserId(Dictionary<string, object> parameters)
    {
        string? result = null;

        if (parameters.TryGetValue("userId", out var userId) && userId is string id)
        {
            result = id;
        }
        else if (parameters.TryGetValue("user", out var user) && user is not null)
        {
            result = ExtractTwitchId(user);
        }

        return result;
    }

    private static string? ExtractTwitchId(object user)
    {
        string? result = null;

        var property = user.GetType().GetProperty("TwitchId");

        if (property?.GetValue(user) is string twitchId)
        {
            result = twitchId;
        }

        return result;
    }
}
