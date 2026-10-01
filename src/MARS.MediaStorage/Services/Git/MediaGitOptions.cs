namespace MARS.MediaStorage.Services.Git;

/// <summary>
/// Конфигурация синхронизации каталога wwwroot с внешним git-репозиторием.
/// По умолчанию выключена: подсистема не должна трогать git, пока оператор
/// явно не включил её и не задал репозиторий.
/// </summary>
public sealed class MediaGitOptions
{
    public const string SectionName = "MediaGit";

    /// <summary>
    /// Мастер-ключ. При <c>false</c> ни одна git-команда не выполняется.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// URL удалённого репозитория. Хранится в <c>.git/config</c> БЕЗ
    /// учётных данных — токен подставляется только в момент обращения к remote.
    /// </summary>
    public string RepositoryUrl { get; set; } = string.Empty;

    public string Branch { get; set; } = "master";

    public string AuthorName { get; set; } = "MARS MediaStorage";

    public string AuthorEmail { get; set; } = "media-storage@localhost";

    /// <summary>
    /// Необязательное имя пользователя для HTTP-аутентификации GitHub.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Токен доступа. Никогда не попадает в <c>.git/config</c>, в аргументы
    /// командной строки и в логи.
    /// </summary>
    public string? Token { get; set; }

    public string RemoteName { get; set; } = "origin";

    /// <summary>
    /// Каталог для метаданных git вне рабочей копии. Нужен, чтобы том с
    /// <c>.git</c> переживал пересборку образа: монтировать пустой volume
    /// прямо на <c>/app/wwwroot/.git</c> нельзя — клон в непустой каталог
    /// wwwroot всё равно не сработает.
    /// </summary>
    public string GitDataDirectory { get; set; } = "/var/lib/mars-media-git";

    /// <summary>
    /// Таймаут одной git-команды. Операции над 1.9 ГБ требуют времени.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 900;

    /// <summary>
    /// Проверяет конфигурацию и возвращает список проблем. Пустой список —
    /// конфигурация пригодна к использованию.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (!Enabled)
        {
            return problems;
        }

        if (string.IsNullOrWhiteSpace(RepositoryUrl))
        {
            problems.Add("MediaGit:RepositoryUrl обязателен при Enabled=true");
        }
        else if (!IsSupportedRepositoryUrl(RepositoryUrl))
        {
            problems.Add(
                "MediaGit:RepositoryUrl должен быть http(s), ssh или локальным путём"
            );
        }

        if (string.IsNullOrWhiteSpace(Branch))
        {
            problems.Add("MediaGit:Branch обязателен при Enabled=true");
        }

        if (Branch.Any(char.IsWhiteSpace) || Branch.Contains("..", StringComparison.Ordinal))
        {
            problems.Add("MediaGit:Branch не должен содержать пробелов или '..'");
        }

        if (string.IsNullOrWhiteSpace(AuthorName))
        {
            problems.Add("MediaGit:AuthorName обязателен");
        }

        if (string.IsNullOrWhiteSpace(AuthorEmail))
        {
            problems.Add("MediaGit:AuthorEmail обязателен");
        }

        if (string.IsNullOrWhiteSpace(GitDataDirectory))
        {
            problems.Add("MediaGit:GitDataDirectory обязателен");
        }

        return problems;
    }

    /// <summary>
    /// Отсекает схемы, которые git трактует как «шелл-команду»:
    /// <c>ext::sh -c ...</c> и <c>fd::</c> выполняют произвольные команды.
    /// </summary>
    private static bool IsSupportedRepositoryUrl(string url)
    {
        if (url.Contains("::", StringComparison.Ordinal))
        {
            return false;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("git@", StringComparison.Ordinal))
        {
            return true;
        }

        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Локальный путь к bare-репозиторию (используется в тестах и зеркалах).
        return !url.Contains("://", StringComparison.Ordinal);
    }
}
