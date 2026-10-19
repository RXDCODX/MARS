using System.Text;

namespace MARS.Shared.Configuration;

/// <summary>
/// Имя службы <c>docker-compose.yml</c> по имени проекта.
/// </summary>
/// <remarks>
/// Нужно локальной отладке на двух шагах наблюдаемости: имя файла цели Prometheus
/// и имя файла логов, который потом читает Grafana Alloy. Оба места обязаны давать
/// то же имя, что и контейнер того же сервиса, — иначе запрос
/// <c>{container="telegram"}</c> нашёл бы логи контейнера, но не логи того же
/// сервиса с хоста.
/// <para>
/// Список соответствий в коде разошёлся бы молча: опечатка в нём выглядит не
/// ошибкой компиляции, а пустым графиком и пустым запросом в Grafana.
/// </para>
/// </remarks>
internal static class ComposeServiceNames
{
    /// <summary>Префикс, с которым названы проекты сервисов.</summary>
    private const string ProjectPrefix = "MARS.";

    /// <summary>
    /// Имя службы compose по имени проекта: <c>MARS.TwitchCore</c> →
    /// <c>twitch-core</c>. <c>null</c>, если имени нет.
    /// </summary>
    /// <remarks>
    /// Слова из одних заглавных не разбиваются по буквам (<c>TTS</c> → <c>tts</c>,
    /// а не <c>t-t-s</c>), а цифры остаются приставлены к слову
    /// (<c>Videos365</c> → <c>videos365</c>): имена служб в compose написаны именно
    /// так, и сверка идёт с ними.
    /// </remarks>
    public static string? From(string? project)
    {
        if (string.IsNullOrWhiteSpace(project))
        {
            return null;
        }

        var name = project.Trim();

        if (name.StartsWith(ProjectPrefix, StringComparison.Ordinal))
        {
            name = name[ProjectPrefix.Length..];
        }

        if (name.Length == 0)
        {
            return null;
        }

        var result = new StringBuilder(name.Length + 4);

        for (var index = 0; index < name.Length; index++)
        {
            var current = name[index];

            if (index > 0 && char.IsUpper(current) && StartsWord(name, index))
            {
                result.Append('-');
            }

            result.Append(char.ToLowerInvariant(current));
        }

        return result.ToString();
    }

    /// <summary>
    /// Заглавная начинает новое слово либо после строчной и цифры
    /// (<c>TwitchCore</c>), либо перед строчной (<c>HTTPClient</c>).
    /// </summary>
    private static bool StartsWord(string name, int index)
    {
        var previous = name[index - 1];

        if (char.IsLower(previous) || char.IsDigit(previous))
        {
            return true;
        }

        return char.IsUpper(previous)
            && index + 1 < name.Length
            && char.IsLower(name[index + 1]);
    }
}
