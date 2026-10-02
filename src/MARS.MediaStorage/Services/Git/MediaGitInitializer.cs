using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Services.Git;

/// <summary>
/// Готовит репозиторий при старте сервиса и поддерживает готовность после
/// временных сетевых сбоев.
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
        if (!options.Enabled)
        {
            logger.LogInformation("Синхронизация git выключена (MediaGit:Enabled=false)");

            return;
        }

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

        var result = await gitService.EnsureInitializedAsync(cancellationToken);

        if (result.Success)
        {
            logger.LogInformation(
                "Репозиторий media-storage готов: branch={Branch}, remote={Remote}",
                options.Branch,
                options.RemoteName
            );

            return;
        }

        logger.LogError(
            "Не удалось подготовить git-репозиторий: {Error}. Медиа продолжают "
                + "отдаваться; следующая попытка будет при первом событии файла.",
            result.Error
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
