using MARS.MediaStorage.Application.Git;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Infrastructure.Git;

/// <summary>
/// Проверяет смонтированную рабочую копию и её Git remote до старта сервиса.
/// </summary>
/// <remarks>
/// Аудит Stage 1: первая версия бросала исключение и при недоступном remote.
/// С контейнером <c>restart: unless-stopped</c> это давало бесконечный
/// crash-loop и уводило вниз не только git, но и отдачу медиа — хранилище
/// переставало работать из-за недоступности чужого репозитория.
/// Теперь некорректная конфигурация (операторская ошибка, не лечится сама) —
/// падение при старте, а сетевой сбой — повтор с нарастающей паузой при
/// продолжающем обслуживании запросов.
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
