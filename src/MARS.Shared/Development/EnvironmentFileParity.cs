namespace MARS.Shared.Development;

/// <summary>
/// Сверка файла окружения с его шаблоном.
/// </summary>
/// <remarks>
/// <para>
/// Перенос ключей руками ломается именно так, как ломался уже не раз
/// (<c>SEQ_ADMIN_PASSWORD</c>, <c>Loki__Url</c>, <c>LogsDb</c>): новый ключ
/// добавлен в шаблон, а в рабочем файле окружения его нет — и compose поднимает
/// стенд с пустым значением, ничего не сообщая.
/// </para>
/// <para>
/// Проверка читает только <b>имена</b> ключей, поэтому результат безопасно
/// ложится в лог: значения рабочего файла — секреты владельца и наружу не идут.
/// Логика эталонная и живёт здесь, а не в скриптах: два скрипта написаны
/// разными языками и друг с другом не сверяются.
/// </para>
/// </remarks>
public static class EnvironmentFileParity
{
    /// <summary>
    /// Ключи шаблона, которых нет в файле окружения: отсортированы и без
    /// повторов — предупреждение читается, а не превращается в поток строк.
    /// </summary>
    /// <param name="file">Файл окружения, где секреты владельца.</param>
    /// <param name="template">Его шаблон — источник ключей.</param>
    public static IReadOnlyList<string> Missing(string file, string template)
    {
        var missing = Array.Empty<string>();

        if (File.Exists(file) && File.Exists(template))
        {
            var present = Keys(File.ReadAllLines(file));
            missing = Keys(File.ReadAllLines(template))
                .Where(key => !present.Contains(key, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();
        }

        return missing;
    }

    /// <summary>
    /// Имена ключей из строк вида <c>ИМЯ=значение</c>. Комментарии и пустые
    /// строки не ключи, а всё, что до первого <c>=</c> — имя (значение сюда не
    /// попадает по построению).
    /// </summary>
    /// <param name="lines">Строки файла окружения.</param>
    public static IReadOnlyList<string> Keys(IEnumerable<string> lines)
    {
        var keys = new List<string>();

        foreach (var line in lines)
        {
            var key = Key(line);

            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static string? Key(string line)
    {
        string? key = null;
        var trimmed = line.Trim();

        if (trimmed.Length > 0 && trimmed[0] != '#')
        {
            var separator = trimmed.IndexOf('=');

            if (separator > 0)
            {
                key = trimmed[..separator].Trim();
            }
        }

        return key;
    }
}
