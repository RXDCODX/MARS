using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services;
using MARS.Shared.Configuration;
using MARS.Shared.Telegram;
using Microsoft.Extensions.Options;

namespace MARS.MediaStorage.Services.Media;

/// <summary>
/// Фоновый проход по файлам мемов: раз в три часа проверяет, что всё
/// проигрывается корректно, и отчитывается в Telegram о перекодированном.
/// </summary>
/// <remarks>
/// Перенос <c>TwitchMediaTranscodeWorker</c> монолита. Перенос делать сюда, а не
/// в MARS.TwitchCore: и мемы, и алерты, и ffmpeg принадлежат
/// MARS.MediaStorage — там же лежит и <c>IsFileNotConvertable</c>.
/// <para>
/// Мессенджер и список администраторов nullable по той же причине, что и в
/// <c>SiteUnavailableNotifier</c>: сервис должен работать без Telegram.
/// </para>
/// </remarks>
public class MemeMediaTranscodeWorker(
    IRandomMemeService memeService,
    MemeMediaPreparationService preparationService,
    ITelegramAdminMessenger? messenger,
    IOptions<TelegramConfig> telegramConfig,
    ILogger<MemeMediaTranscodeWorker> logger,
    TimeProvider? timeProvider = null
) : BackgroundService
{
    /// <summary>Интервал прохода. В монолите — три часа.</summary>
    public static readonly TimeSpan PassInterval = TimeSpan.FromHours(3);

    private readonly SemaphoreSlim _runLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (await _runLock.WaitAsync(0, stoppingToken))
        {
            try
            {
                await RunPassAsync(stoppingToken);
            }
            finally
            {
                _runLock.Release();
            }
        }

        using var timer = new PeriodicTimer(PassInterval, timeProvider ?? TimeProvider.System);

        while (
            !stoppingToken.IsCancellationRequested
            && await timer.WaitForNextTickAsync(stoppingToken)
        )
        {
            // Проход не накладывается на себя: перекодирование длиннее интервала,
            // и два прохода в одной службе жгли бы CPU вдвое.
            if (!await _runLock.WaitAsync(0, stoppingToken))
            {
                continue;
            }

            try
            {
                await RunPassAsync(stoppingToken);
            }
            finally
            {
                _runLock.Release();
            }
        }
    }

    /// <summary>
    /// Один проход: подготовить каждый конвертируемый файл и отправить сводку.
    /// </summary>
    public async Task RunPassAsync(CancellationToken cancellationToken)
    {
        var reports = new List<string>();
        var inspected = 0;

        try
        {
            var memeOrders = (await memeService.GetAllMemeOrdersAsync(cancellationToken)).ToList();

            foreach (var order in memeOrders.Where(order => !order.IsFileNotConvertable))
            {
                inspected++;

                try
                {
                    var result = await preparationService.PrepareAsync(
                        order.FilePath,
                        null,
                        report =>
                        {
                            reports.Add(report);
                            return Task.CompletedTask;
                        },
                        cancellationToken
                    );

                    if (!result.FileExists)
                    {
                        logger.LogWarning(
                            "Файл мема не найден при перекодировании: {FilePath}",
                            order.FilePath
                        );
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Ошибка обработки файла {FilePath}", order.FilePath);
                }
            }

            if (reports.Count > 0)
            {
                await SendSummaryAsync(
                    MemeMediaTranscodePolicy.BuildBatchSummary(inspected, reports),
                    cancellationToken
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Проход перекодирования прерван остановкой сервиса");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось выполнить фоновую перекодировку медиа");
        }
    }

    private async Task SendSummaryAsync(string summary, CancellationToken cancellationToken)
    {
        var admins = telegramConfig.Value?.AdminIds ?? [];

        if (messenger is null || admins.Length == 0)
        {
            logger.LogInformation(
                "Сводка о перекодировании не отправлена: messenger={Configured}, администраторов={Admins}",
                messenger is not null,
                admins.Length
            );

            return;
        }

        var parts = MemeMediaTranscodePolicy.SplitForTelegram(summary);

        foreach (var adminId in admins)
        {
            foreach (var part in parts)
            {
                try
                {
                    await messenger.SendAsync(adminId, part, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Не удалось отправить сводку о перекодировании администратору {AdminId}",
                        adminId
                    );
                }
            }
        }
    }
}
