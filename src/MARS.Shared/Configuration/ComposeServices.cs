using System.Text.RegularExpressions;

namespace MARS.Shared.Configuration;

/// <summary>
/// Имена служб из файлов compose.
/// </summary>
/// <remarks>
/// Нужно, чтобы не объявлять цель наблюдаемости сервису, которого в compose нет.
/// Такой сервис один — MARS.AudioController управляет микшером Windows и
/// не контейнеризуется, — но проверять это специально не нужно: сверка идёт с
/// самим compose, как и сопоставление переменных окружения в
/// <see cref="ComposeEnvironmentKeys"/>.
/// </remarks>
internal static class ComposeServices
{
    private const string ServicesSection = "services";

    /// <summary>
    /// Файлы читаются по порядку, поэтому оверрайд дополняет основной файл — так
    /// же, как их видит сам compose.
    /// </summary>
    private static readonly string[] FileNames = ["docker-compose.yml", "docker-compose.dev.yml"];

    /// <summary>Верхнеуровневый раздел: <c>services:</c>, <c>networks:</c>, <c>volumes:</c>.</summary>
    private static readonly Regex SectionPattern = new(
        @"^(?<name>[A-Za-z0-9_.-]+):\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Служба раздела: ровно две позиции отступа. Вложенные ключи уходят глубже,
    /// а у сетей и томов отступ такой же — поэтому разделы отслеживаются отдельно.
    /// </summary>
    private static readonly Regex ServicePattern = new(
        @"^  (?<name>[A-Za-z0-9][A-Za-z0-9_.-]*):\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Имена служб из файлов compose в каталоге. Отсутствие каталога или файлов —
    /// не ошибка: результат просто пуст, и решение принимает вызывающий.
    /// </summary>
    public static HashSet<string> Read(string? directory)
    {
        var services = new HashSet<string>(StringComparer.Ordinal);

        if (directory is null)
        {
            return services;
        }

        foreach (var fileName in FileNames)
        {
            var path = Path.Combine(directory, fileName);

            if (File.Exists(path))
            {
                ReadFile(path, services);
            }
        }

        return services;
    }

    private static void ReadFile(string path, HashSet<string> services)
    {
        var inside = false;

        foreach (var line in File.ReadLines(path))
        {
            var section = SectionPattern.Match(line);

            if (section.Success)
            {
                inside = string.Equals(
                    section.Groups["name"].Value,
                    ServicesSection,
                    StringComparison.Ordinal
                );

                continue;
            }

            if (inside)
            {
                var service = ServicePattern.Match(line);

                if (service.Success)
                {
                    services.Add(service.Groups["name"].Value);
                }
            }
        }
    }
}