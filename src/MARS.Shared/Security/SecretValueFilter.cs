namespace MARS.Shared.Security;

/// <summary>
/// Определяет, относится ли ключ конфигурации/переменной окружения к секретам.
/// </summary>
/// <remarks>
/// Используется в <c>MARS.Admin</c> при автозаполнении таблицы
/// <c>environment_variables</c> из окружения процесса: без фильтрации в базу
/// попадали <c>ServiceAuth__ApiKey</c>, <c>ConnectionStrings__*</c>,
/// <c>POSTGRES_PASSWORD</c> и прочие учётные данные, которые затем отдавались
/// неаутентифицированным <c>GET /api/EnvironmentVariable</c> (блокер №6 аудита).
/// </remarks>
public static class SecretValueFilter
{
    /// <summary>
    /// Нормализованные фрагменты имён, указывающие на секрет.
    /// Ключ предварительно очищается от всех не-буквенно-цифровых символов
    /// и приводится к нижнему регистру, поэтому <c>SERVICE_API_KEY</c>,
    /// <c>ServiceAuth__ApiKey</c> и <c>service-auth:api-key</c> совпадают одинаково.
    /// </summary>
    private static readonly string[] SecretMarkers =
    [
        "password",
        "passwd",
        "pwd",
        "secret",
        "token",
        "apikey",
        "accesstoken",
        "authtoken",
        "bearer",
        "connectionstring",
        "privatekey",
        "credential",
        "clientid",
        "cert",
        "signature",
        "license",
        "dsn",
    ];

    /// <summary>
    /// Возвращает <c>true</c>, если имя ключа похоже на имя секрета.
    /// </summary>
    public static bool IsSecretKey(string? key)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(key))
        {
            var normalized = Normalize(key);
            result = SecretMarkers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal));
        }

        return result;
    }

    /// <summary>
    /// Возвращает только те пары ключ/значение, которые не являются секретами.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string?>> Filter(
        IEnumerable<KeyValuePair<string, string?>> source
    )
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = source
            .Where(entry => !IsSecretKey(entry.Key))
            .ToList();

        return result;
    }

    private static string Normalize(string key)
    {
        var buffer = new char[key.Length];
        var length = 0;

        foreach (var symbol in key)
        {
            if (char.IsLetterOrDigit(symbol))
            {
                buffer[length++] = char.ToLowerInvariant(symbol);
            }
        }

        var result = new string(buffer, 0, length);
        return result;
    }
}
