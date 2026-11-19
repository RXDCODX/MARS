using System.Text;
using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.RandomMemes;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegram;
using Microsoft.Extensions.Options;

namespace MARS.MediaStorage.Infrastructure.Media;

/// <summary>
/// Выдаёт следующий звук очереди, подготавливает его для overlay и сообщает
/// администраторам о перекодировании, как это делала старая Twitch-награда.
/// </summary>
public sealed class RandomMemePlaybackService(
    IRandomMemeService memeService,
    MemeMediaPreparationService preparationService,
    ITelegramAdminMessenger? messenger,
    IOptions<TelegramConfig> telegramConfig,
    ILogger<RandomMemePlaybackService> logger
) : IRandomMemePlaybackService
{
    public async Task<MediaInfo?> GetNextPreparedMediaAsync(
        string folderPath,
        string? displayName,
        CancellationToken cancellationToken = default
    )
    {
        var memeOrder = await memeService.GetNextMemeOrderAsync(folderPath, cancellationToken);
        if (memeOrder?.Type is null)
        {
            return null;
        }

        var normalizedFolder = MediaPath.Normalize(memeOrder.Type.FolderPath);
        var normalizedFile = MediaPath.Normalize(memeOrder.FilePath);
        var sourcePath = normalizedFile.StartsWith(
            normalizedFolder + "/",
            StringComparison.OrdinalIgnoreCase
        )
            ? normalizedFile
            : MediaPath.Combine(normalizedFolder, normalizedFile);

        var transcodeReports = new List<string>();
        var prepared = await preparationService.PrepareAsync(
            sourcePath,
            displayName,
            report =>
            {
                transcodeReports.Add(report);
                return Task.CompletedTask;
            },
            cancellationToken
        );

        if (!prepared.FileExists || prepared.Media is null)
        {
            logger.LogWarning(
                "Файл из очереди {FolderPath} не найден или не подготовлен: {FilePath}",
                folderPath,
                sourcePath
            );
            return null;
        }

        if (transcodeReports.Count > 0)
        {
            await SendTranscodeNotificationAsync(
                BuildSingleFileSummary(sourcePath, transcodeReports),
                cancellationToken
            );
        }

        return prepared.Media;
    }

    private static string BuildSingleFileSummary(
        string sourcePath,
        IReadOnlyList<string> transcodeReports
    )
    {
        var result = new StringBuilder();
        result.AppendLine("Обработка файлов завершена");
        result.AppendLine("Всего файлов: 1");
        result.AppendLine($"Требовали конвертацию: {transcodeReports.Count}");
        result.AppendLine("Полный список:");
        result.AppendLine($"1. {sourcePath}");

        foreach (var report in transcodeReports)
        {
            result.AppendLine();
            result.AppendLine(report);
        }

        return result.ToString().TrimEnd();
    }

    private async Task SendTranscodeNotificationAsync(
        string message,
        CancellationToken cancellationToken
    )
    {
        var adminIds = telegramConfig.Value.AdminIds ?? [];
        if (messenger is null || adminIds.Length == 0)
        {
            return;
        }

        foreach (var adminId in adminIds)
        {
            foreach (var part in MemeMediaTranscodePolicy.SplitForTelegram(message))
            {
                try
                {
                    await messenger.SendAsync(adminId, part, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(
                        exception,
                        "Не удалось отправить уведомление о подготовке медиа администратору {AdminId}",
                        adminId
                    );
                }
            }
        }
    }
}
