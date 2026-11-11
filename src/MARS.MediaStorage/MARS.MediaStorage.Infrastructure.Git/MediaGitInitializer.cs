using MARS.MediaStorage.Application.Git;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Infrastructure.Git;

/// <summary>
/// Проверяет смонтированную рабочую копию и её Git remote до старта сервиса.
/// </summary>
/// <remarks>
/// Проверка полностью локальная и не зависит от доступности GitHub. При
/// отсутствии рабочей копии или несовпадении remote приложение не должно
/// запускаться: иначе Alerts и OBS могли бы писать файлы в неверный каталог.
/// </remarks>
public sealed class MediaGitInitializer(
    IMediaGitService gitService,
    MediaGitOptions options,
    ILogger<MediaGitInitializer> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var problems = options.Validate();

        if (problems.Count > 0)
        {
            foreach (var problem in problems)
            {
                logger.LogError("Некорректная конфигурация git: {Problem}", problem);
            }

            throw new InvalidOperationException(
                "Некорректная конфигурация MediaGit: " + string.Join("; ", problems)
            );
        }

        var result = await gitService.ValidateRepositoryAsync(cancellationToken);

        if (!result.Success)
        {
            logger.LogCritical(
                "Неверный каталог MediaStorage: {Error}. Приложение не будет запущено.",
                result.Error
            );

            throw new InvalidOperationException(
                "Проверка MEDIA_STORAGE_HOST_PATH не пройдена: " + result.Error
            );
        }

        if (!options.Enabled)
        {
            logger.LogInformation(
                "Рабочая копия media-storage проверена: remote={Remote}. "
                    + "Git-синхронизация выключена (MediaGit:Enabled=false).",
                options.RemoteName
            );

            return;
        }

        logger.LogInformation(
            "Рабочая копия media-storage проверена: branch={Branch}, remote={Remote}",
            options.Branch,
            options.RemoteName
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
