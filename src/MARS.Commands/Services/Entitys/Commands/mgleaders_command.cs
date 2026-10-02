using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;

namespace MARS.Commands.Services.Entitys.Commands;

/// <summary>
/// Топ победителей мини-игр. Данные лежат в MARS.TwitchCore (таблица
/// <c>TwitchLeaderboardUsers</c> его базы), поэтому команда ходит туда по
/// внутреннему API.
/// </summary>
public class MGLeadersCommand(ILeaderboardClient leaderboardClient) : BaseCommand
{
    private const int TopCount = 3;

    public override string CommandName => "mgleaders";
    public override string Description => "Показывает топ победителей мини-игр";
    public override bool IsAdminCommand => false;

    public override Platform[] AvailablePlatforms =>
        [Platform.Twitch, Platform.Telegram, Platform.Api];

    public override async Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platform = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var top = await leaderboardClient.GetTopAsync(TopCount, cancellationToken);

        if (top is null)
        {
            return CommandResult.Fail(
                "Не удалось получить таблицу лидеров: сервис недоступен.",
                CommandErrorCode.TargetUnreachable
            );
        }

        if (top.Entries.Count == 0)
        {
            return CommandResult.Ok("Данные о победителях пока отсутствуют.");
        }

        var parts = top.Entries.Select(
            (entry, index) =>
                $"{index + 1}. {entry.DisplayName ?? entry.TwitchId} — {entry.TotalWins} побед "
                + $"(рулетка: {entry.RussianRouletteWins}, викторина: {entry.TriviaWins})"
        );

        return CommandResult.Ok("Топ мини-игр: " + string.Join("; ", parts));
    }
}
