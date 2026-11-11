namespace MARS.MediaStorage.Infrastructure.Git;

/// <summary>
/// Конфигурация рабочей копии wwwroot и её синхронизации с внешним Git.
/// Проверка локального репозитория обязательна всегда; Enabled управляет
/// только commit/push, а не проверкой точки монтирования.
/// </summary>
public sealed class MediaGitOptions
{
    public const string SectionName = "MediaGit";

    /// <summary>
    /// Мастер-ключ. При <c>false</c> локальная рабочая копия всё равно проверяется,
    /// но операции commit/push не выполняются.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Ожидаемый URL remote. Сверяется с fetch и push URL локальной рабочей копии.
    /// Учётные данные не должны храниться в <c>.git/config</c>.
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
    /// Проверяет конфигурацию и возвращает список проблем. Пустой список —
    /// конфигурация пригодна к использованию.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        // RepositoryUrl нужен всегда: даже при выключенном push приложение
        // обязано проверить, что смонтирована именно ожидаемая рабочая копия.
        if (string.IsNullOrWhiteSpace(RepositoryUrl))
        {
            problems.Add("MediaGit:RepositoryUrl обязателен для проверки локальной рабочей копии");
        }
        else if (!IsSupportedRepositoryUrl(RepositoryUrl))
        {
            problems.Add("MediaGit:RepositoryUrl должен быть http(s), ssh или локальным путём");
        }

        if (string.IsNullOrWhiteSpace(RemoteName))
        {
            problems.Add("MediaGit:RemoteName обязателен");
        }

        if (!Enabled)
        {
            return problems;
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

        if (
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        if (
            url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("git@", StringComparison.Ordinal)
        )
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
