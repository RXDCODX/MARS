using MARS.TwitchCore.Data;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Entities.Subs;

public class RouleteGame(
    List<RouletePlayer> players,
    GameType type,
    ITwitchClient client,
    ILogger logger,
    IDbContextFactory<TwitchDbContext> factory,
    IWaifuLookupService waifuLookupService,
    CancellationToken token
)
{
    public bool IsGameRunning { get; set; }
    private const int ChanceToBeSaved = 40;
    private List<RouletePlayer> Players { get; } = [.. players];
    private GameType Type { get; } = type;
    private readonly List<string> _noWaifuHelpUsers = [];

    public async Task RussianRoulette()
    {
        var numPlayers = Players.Count;
        var roundNum = 1;
        var random = new Random();

        if (numPlayers == 1)
        {
            await AloneRoulette(Players[0].Name);
            IsGameRunning = false;
            return;
        }

        if (numPlayers == 2)
        {
            var namesForMinigame = string.Join(", ", Players.Select(player => player.Name));
            await client.SendMessageToMainTwitchAsync(
                $"Играется рулетка на двоих! Играют: {namesForMinigame}",
                logger
            );
        }

        while (Players.Count(e => e.IsAlive) > 1 && !token.IsCancellationRequested)
        {
            var alivePlayers = Players.Where(player => player.IsAlive).ToList();
            if (Type != GameType.MiniGame)
            {
                var alivePlayersNames = string.Join(
                    ", ",
                    alivePlayers.Select(player => player.Name)
                );
                await client.SendMessageToMainTwitchAsync(
                    $"Русская рулетка - раунд {roundNum}! Играют: {alivePlayersNames}",
                    logger
                );
            }

            var index = random.Next(alivePlayers.Count(e => e.IsAlive));
            await Task.Delay(1000, token);
            RouletePlayer shotPlayer = alivePlayers[index];

            var isSaved = await TryToSavePlayer(shotPlayer);

            if (isSaved)
            {
                var waifuName = await waifuLookupService.GetWaifuNameForUserAsync(
                    shotPlayer.TwitchId,
                    token
                );
                var saveMessage = string.IsNullOrWhiteSpace(waifuName)
                    ? $"@{shotPlayer.Name}, твой супруг спас тебя от неминуемой гибели!"
                    : $"@{shotPlayer.Name}, {waifuName} спас(ла) тебя от неминуемой гибели!";

                await client.SendMessageToMainTwitchAsync(saveMessage, logger);
            }
            else
            {
                await client.SendMessageToMainTwitchAsync(
                    StaticContent.PlayerEliminated(shotPlayer.Name),
                    logger
                );
                shotPlayer.IsAlive = false;
            }

            await Task.Delay(2000, token);

            roundNum++;
        }

        RouletePlayer winner = Players.First(e => e.IsAlive);
        if (Type == GameType.MiniGame)
        {
            await client.SendMessageToMainTwitchAsync(
                $"Победитель: {winner.Name}. {StaticContent.GetMiniHistory(winner.Name)}",
                logger
            );
        }
        else
        {
            await client.SendMessageToMainTwitchAsync(
                $"Поздравляем {winner.Name} с победой в игре!",
                logger
            );
        }

        IsGameRunning = false;
    }

    private async ValueTask<bool> TryToSavePlayer(RouletePlayer shotPlayer)
    {
        if (_noWaifuHelpUsers.Contains(shotPlayer.TwitchId))
        {
            return false;
        }
        await using var dbcontext = await factory.CreateDbContextAsync(token);
        Husband? host = await dbcontext.Husbands.FindAsync(shotPlayer.TwitchId);

        if (host?.IsPrivated == false)
        {
            return false;
        }

        var chance = Random.Shared.Next(0, 101);
        if (chance < ChanceToBeSaved)
        {
            _noWaifuHelpUsers.Add(shotPlayer.TwitchId);
            return true;
        }

        return false;
    }

    private async Task AloneRoulette(string username)
    {
        await client.SendMessageToMainTwitchAsync($"@{username}, я взвожу курок...", logger);
        await Task.Delay(3000, token);
        await client.SendMessageToMainTwitchAsync($"@{username}, 3", logger);
        await Task.Delay(1000, token);
        await client.SendMessageToMainTwitchAsync($"@{username}, 2", logger);
        await Task.Delay(1000, token);
        await client.SendMessageToMainTwitchAsync($"@{username}, 1", logger);
        await Task.Delay(1000, token);

        var rnd = new Random();
        var randomShoot = rnd.Next(1, 7);
        switch (randomShoot)
        {
            case 1:
                await client.SendMessageToMainTwitchAsync(
                    $"@{username}, сегодня твой день.",
                    logger
                );
                break;
            case 6:
                await client.SendMessageToMainTwitchAsync(
                    $"@{username}, осечка, но я не думаю, что в следующий раз тебе так повезет.",
                    logger
                );
                break;
            case 3:
                await client.SendMessageToMainTwitchAsync(
                    $"@{username}, я медленно подвожу ствол к твоему виску. Ничего не происходит. Повезло. Или это просто осечка?",
                    logger
                );
                break;
            case 4:
                await client.SendMessageToMainTwitchAsync(
                    $"@{username}, повезло. Не уверен, что ты рискнешь еще раз со мной сыграть в эту игру.",
                    logger
                );
                break;
            case 5:
                await client.SendMessageToMainTwitchAsync(
                    $"@{username}, живой или мертвый ты пойдешь со мной. Но видимо не сегодня.",
                    logger
                );
                break;
            case 2:
                await client.SendMessageToMainTwitchAsync(
                    $"@{username}, BANG! BANG! BANG!",
                    logger
                );
                await client.SendMessageToMainTwitchAsync(
                    $"/timeout {username} 600 Проиграл(а)_в_русскую_рулетку!",
                    logger
                );
                break;
        }
    }
}
