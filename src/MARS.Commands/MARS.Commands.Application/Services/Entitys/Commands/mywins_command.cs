using Grpc.Core;
using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;

namespace MARS.Commands.Application.Services.Entitys.Commands;

/// <summary>
/// Победы вызывающего пользователя в мини-играх. Таблица лежит в
/// MARS.TwitchCore, поэтому команда ходит туда по внутреннему API.
/// </summary>
public class MyWinsCommand(IMarsGrpcClient grpcClient) : BaseCommand
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

        MARS.Shared.Contracts.Clients.LeaderboardStats? stats;
        try
        {
            stats = await grpcClient.Twitch.GetUserStatsAsync(twitchId, cancellationToken);
        }
        catch (RpcException)
        {
            stats = null;
        }

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
