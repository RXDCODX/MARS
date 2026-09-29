namespace MARS.Shared.Configuration;

/// <summary>
/// Настройки аутентификации межсервисных REST-эндпоинтов.
/// Ключ передаётся в заголовке <c>X-Api-Key</c> (или <c>Api-Key</c>).
/// Если <see cref="ApiKey"/> пустой, схема остаётся неактивной и эндпоинты открыты —
/// это допустимо только для одиночного локального запуска.
/// </summary>
public class ServiceAuthOptions
{
    public const string SectionName = "ServiceAuth";

    public const string DefaultApiKeyHeaderName = "X-Api-Key";

    public string? ApiKey { get; set; }
    public string ApiKeyHeaderName { get; set; } = DefaultApiKeyHeaderName;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiKey);
}
