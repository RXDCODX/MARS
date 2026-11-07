using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Services.Twitch.Rewards;

public class RandomMemeWorker(
    IWebHostEnvironment webHostEnvironment,
    ILogger<RandomMemeWorker> logger
) : BackgroundService
{
    private const string CacheFolderName = "_converted";
    public bool IsServiceActive { get; set; } = true;
    private readonly string _folderPath = Path.Combine(
        webHostEnvironment.WebRootPath ?? webHostEnvironment.ContentRootPath,
        "Alerts"
    );

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && IsServiceActive)
        {
            try
            {
                if (Directory.Exists(_folderPath))
                {
                    var files = Directory
                        .GetFiles(_folderPath, "*", SearchOption.AllDirectories)
                        .Where(filePath => !IsCacheFile(filePath))
                        .ToArray();

                    logger.LogDebug(
                        "RandomMemeWorker: found {Count} files in {Path}",
                        files.Length,
                        _folderPath
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scanning meme folder");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private static bool IsCacheFile(string filePath)
    {
        var result = filePath.Contains(
            Path.DirectorySeparatorChar + CacheFolderName + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase
        );

        if (!result)
        {
            result = filePath.Contains(
                Path.AltDirectorySeparatorChar + CacheFolderName + Path.AltDirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            );
        }

        return result;
    }
}
