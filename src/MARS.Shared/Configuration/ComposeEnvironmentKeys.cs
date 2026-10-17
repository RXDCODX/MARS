using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MARS.Shared.Configuration;

/// <summary>
/// Соответствие между именами переменных файла окружения и ключами конфигурации.
/// </summary>
/// <remarks>
/// <para>
/// Имена в <c>.env</c> — это имена переменных compose (<c>TWITCH_OAUTH</c>), а
/// конфигурации нужен ключ секции (<c>Twitch:OAuth</c>). Сопоставление объявлено в
/// <c>docker-compose.yml</c> строками вида <c>Twitch__OAuth: ${TWITCH_OAUTH:-}</c>,
/// и это единственное место, где оно записано: дублировать его списком значило бы
/// завести третью копию правды, которая разъедется молча.
/// </para>
/// <para>
/// Имя без сопоставления добавляется как есть — ровно так же оно и дошло бы до
/// контейнера: compose передал бы его переменной окружения, а .NET положил бы
/// значение в одноимённый ключ конфигурации.
/// </para>
/// </remarks>
internal static class ComposeEnvironmentKeys
{
    /// <summary>
    /// Строка вида <c>Ключ__Конфигурации: ${ИМЯ_ПЕРЕМЕННОЙ}</c> с необязательным
    /// значением по умолчанию <c>:-</c> и необязательными кавычками. Значение
    /// должно быть ровно подстановкой: строки подключения с подстановкой внутри
    /// (<c>Password=${MARS_TWITCH_PASSWORD}</c>) сопоставлением не являются.
    /// </summary>
    private const string MappingPatternText = """
        ^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*"?\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)(?::-[^}]*)?\}"?\s*$
        """;

    private static readonly Regex MappingPattern = new(
        MappingPatternText,
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Файлы читаются по порядку, поэтому значения оверрайда заменяют значения
    /// основного файла — так же, как это делает сам compose.
    /// </summary>
    private static readonly string[] FileNames = ["docker-compose.yml", "docker-compose.dev.yml"];

    /// <summary>
    /// Читает сопоставление из файлов compose в каталоге. Отсутствие каталога или
    /// файлов — не ошибка: без них имена попадут в конфигурацию как есть.
    /// </summary>
    public static Dictionary<string, string> Read(string? directory)
    {
        var keys = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        if (directory is null)
        {
            return keys;
        }

        foreach (var fileName in FileNames)
        {
            var path = Path.Combine(directory, fileName);

            if (File.Exists(path))
            {
                ReadFile(path, keys);
            }
        }

        return keys;
    }

    private static void ReadFile(string path, Dictionary<string, string> keys)
    {
        foreach (var line in File.ReadLines(path))
        {
            var match = MappingPattern.Match(line);

            if (match.Success)
            {
                keys[match.Groups["name"].Value] = match.Groups["key"].Value;
            }
        }
    }
}
