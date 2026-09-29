using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Extensions;

/// <summary>
/// Резолвер строки подключения: читает DefaultConnection (или указанное имя) из конфигурации
/// и подменяет Password_FILE=&lt;path&gt; фактическим содержимым секретного файла (docker-compose secrets).
/// Используется и в runtime, и в design-time (dotnet ef) — поведение одинаковое.
/// </summary>
public static class MarsConnectionStringResolver
{
    private static readonly Regex PasswordFilePattern = new(
        @"\bPassword_FILE\s*=\s*(?<path>[^;\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public static string? Resolve(
        IConfiguration configuration,
        string connectionName = "DefaultConnection"
    )
    {
        var connectionString = configuration.GetConnectionString(connectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return ResolvePasswordFile(connectionString);
    }

    public static string ResolvePasswordFile(string connectionString)
    {
        if (!connectionString.Contains("Password_FILE", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        return PasswordFilePattern.Replace(
            connectionString,
            match =>
            {
                var path = match.Groups["path"].Value.Trim();

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        $"Строка подключения ссылается на Password_FILE='{path}', но файл не существует. "
                            + "Создайте secrets/db_password.txt (см. secrets/README.md).",
                        path
                    );
                }

                return $"Password={Quote(File.ReadAllText(path).TrimEnd('\r', '\n'))}";
            }
        );
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
