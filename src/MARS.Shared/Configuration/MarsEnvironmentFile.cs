using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Configuration;

/// <summary>
/// Файл окружения стенда как источник конфигурации для запуска на хосте.
/// </summary>
/// <remarks>
/// <para>
/// Файл окружения читает compose — и на этом всё. Сервис, запущенный на хосте
/// (F5), его не видел: значения приходят из <c>launchSettings.json</c>, а секреты
/// туда не кладутся намеренно. Разрыв и был причиной того, что токены Twitch и
/// настройки Telegram приходилось задавать заново и вручную.
/// </para>
/// <para>
/// Имена переменных файла — имена compose (<c>TWITCH_OAUTH</c>), а конфигурации
/// нужен ключ секции (<c>Twitch:OAuth</c>). Перевод берётся из
/// <c>docker-compose.yml</c> — см. <see cref="ComposeEnvironmentKeys"/>, — а двойное
/// подчёркивание в нём заменяется на двоеточие.
/// </para>
/// <para>
/// Файл <em>дозаполняет</em> конфигурацию: в неё попадают только те ключи, которых
/// там ещё нет. Значения ставятся прямо в конфигурацию, а не добавлением
/// провайдера: <c>ConfigurationManager</c> хоста применяет добавленный источник
/// иначе, чем <c>ConfigurationBuilder</c> из тестов, и на живом запуске провайдер
/// из файла молча не появлялся. Прямая запись ключей приоритетом не конфликтует —
/// спор идёт только об отсутствующих ключах.
/// </para>
/// <para>
/// Читается только в Development: в контейнерах боевого стенда значения приходят
/// переменными окружения, а сам файл туда не попадает по <c>.dockerignore</c>.
/// </para>
/// </remarks>
internal static class MarsEnvironmentFile
{
    /// <summary>Имена файлов по приоритету: сначала своя среда, затем общий.</summary>
    private static readonly string[] FileNames = [".env.development", ".env"];

    /// <summary>
    /// Подключает файл, если среда — Development и файл нашёлся. Отсутствие
    /// файла — не ошибка: контейнеру он не нужен вовсе.
    /// </summary>
    /// <param name="configuration">Конфигурация сервиса.</param>
    /// <param name="contentRootPath">
    /// Каталог содержимого приложения: файл лежит в корне репозитория, то есть
    /// выше по дереву, чем каталог сервиса.
    /// </param>
    /// <param name="isDevelopment">Текущая среда.</param>
    /// <returns>Путь прочитанного файла или <c>null</c>.</returns>
    public static string? AddIfPresent(
        IConfigurationManager configuration,
        string contentRootPath,
        bool isDevelopment
    )
    {
        string? added = null;

        if (isDevelopment)
        {
            added = Find(contentRootPath);
        }

        if (added is null)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(added);
        var mappedKeys = ComposeEnvironmentKeys.Read(directory);

        foreach (var pair in Read(added))
        {
            var key = ToConfigurationKey(mappedKeys.GetValueOrDefault(pair.Key, pair.Key));

            if (configuration[key] is null)
            {
                configuration[key] = pair.Value;
            }
        }

        return added;
    }

    /// <summary>
    /// Переводит имя в записи переменной окружения в запись конфигурации:
    /// разделитель секций в конфигурации — двоеточие, а двойное подчёркивание
    /// осталось бы просто частью имени. Индексы массивов переводятся тем же
    /// правилом: <c>Telegram__AdminIds__0</c> — это <c>Telegram:AdminIds:0</c>.
    /// </summary>
    public static string ToConfigurationKey(string key) =>
        key.Replace("__", ":", StringComparison.Ordinal);

    /// <summary>Ищет файл вверх по дереву от каталога содержимого.</summary>
    private static string? Find(string contentRootPath)
    {
        var directory = new DirectoryInfo(contentRootPath);

        while (directory is not null)
        {
            foreach (var name in FileNames)
            {
                var candidate = Path.Combine(directory.FullName, name);

                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Разбор файла в формате compose: комментарии и пустые строки пропускаются,
    /// кавычки и префикс <c>export</c> снимаются, значение режется по первому
    /// знаку <c>=</c> — иначе строки подключения распались бы на части.
    /// </summary>
    public static Dictionary<string, string?> Read(string path)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("export ", StringComparison.Ordinal))
            {
                trimmed = trimmed["export ".Length..].Trim();
            }

            var separator = trimmed.IndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            var key = trimmed[..separator].Trim();

            if (key.Length == 0)
            {
                continue;
            }

            values[key] = Unquote(trimmed[(separator + 1)..].Trim());
        }

        return values;
    }

    private static string Unquote(string value)
    {
        var result = value;

        if (result.Length >= 2)
        {
            var quote = result[0];

            if ((quote == '"' || quote == '\'') && result[^1] == quote)
            {
                result = result[1..^1];
            }
        }

        return result;
    }
}
