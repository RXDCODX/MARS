using MARS.Scoreboard.Grpc;
using MARS.Scoreboard.Hubs.Interfaces;
using MARS.Scoreboard.Services;
using MARS.Shared.Grpc.Scoreboard;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Scoreboard.Hubs;

/// <summary>
/// Хаб табло: команды панели администратора.
/// </summary>
/// <remarks>
/// Состояния подписчикам не отсюда: их рассылает
/// <see cref="ScoreboardHubRelay"/> из широковещателя. Держать вторую подписку
/// в хабе незачем — событие пошло бы в два канала, и при медленном клиенте
/// один из них переполнился бы вхолостую.
/// <para>
/// Команды вызывают тот же <see cref="ScoreboardService"/>, что и
/// REST-контроллер и gRPC-сервис. Отдельной логики здесь нет намеренно: три
/// пути к одному состоянию должны вести к одному результату, иначе правка в
/// одном из них разъезжалась бы с остальными.
/// </para>
/// </remarks>
public class ScoreboardHub(Services.ScoreboardService scoreboardService) : Hub<IScoreboardHub>
{
    /// <summary>
    /// Заменяет состояние табло целиком.
    /// </summary>
    /// <remarks>
    /// Имя вызова совпадает с тем, что шлёт клиент: так контракт хаба и
    /// клиентский код не расходятся из-за переименования на одной стороне.
    /// </remarks>
    public async Task UpdateState(ScoreboardSnapshot state)
    {
        await scoreboardService.UpdateStateAsync(ScoreboardGrpcMapper.ToDto(state));
    }

    /// <summary>Показывает или прячет табло.</summary>
    public async Task<bool> SetVisibility(bool isVisible)
    {
        var result = await scoreboardService.SetVisibilityAsync(isVisible);

        return result;
    }

    /// <summary>Меняет счёт игрока. Номер — 1 или 2.</summary>
    public async Task<bool> UpdatePlayerScore(int playerPosition, int newScore)
    {
        var result = await scoreboardService.UpdatePlayerScoreAsync(playerPosition, newScore);

        return result;
    }

    /// <summary>Проставляет игроку итоговый счёт.</summary>
    public async Task<bool> SetPlayerFinal(int playerPosition, string final)
    {
        var result = await scoreboardService.SetPlayerFinalAsync(playerPosition, final);

        return result;
    }
}
