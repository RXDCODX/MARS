using System.Diagnostics;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Закрывает игровые процессы так же, как это делал обработчик монолита:
/// сначала просит окно закрыться, затем убивает процесс.
/// </summary>
/// <remarks>
/// Имена процессов — <c>Polaris-Win64-Shipping</c> (UE-сборка) и <c>dota2</c>.
/// Ошибка по одному процессу не отменяет остальные: недоступный процесс — это
/// норма, а не повод не закрыть игру.
/// </remarks>
public sealed class GameProcessCloser(ILogger<GameProcessCloser> logger) : IGameCloser
{
    private static readonly string[] ProcessNames = ["Polaris-Win64-Shipping", "dota2"];

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        foreach (var name in ProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try
                {
                    using (process)
                    {
                        process.CloseMainWindow();
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error closing process {ProcessName}", name);
                }
            }
        }

        return Task.CompletedTask;
    }
}
