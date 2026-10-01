using MARS.Shared.Security;

namespace MARS.Shared.Tests.Security;

/// <summary>
/// Блокер №6: <c>ConfigurationKeysBootstrapHostedService</c> копировал всё окружение
/// процесса в таблицу <c>admin.environment_variables</c>, а GET /api/EnvironmentVariable
/// отдавал её без аутентификации — это эндпоинт утечки всех секретов стека.
/// Фильтр должен отсекать ключи-секреты в любом регистре и с любыми разделителями.
/// </summary>
public class SecretValueFilterTests
{
    [Theory]
    [InlineData("ServiceAuth__ApiKey")]
    [InlineData("serviceauth__apikey")]
    [InlineData("SERVICE_API_KEY")]
    [InlineData("ConnectionStrings__DefaultConnection")]
    [InlineData("POSTGRES_PASSWORD")]
    [InlineData("RABBITMQ_PASSWORD")]
    [InlineData("Telegram__BotToken")]
    [InlineData("GitHubToken")]
    [InlineData("AWS_SECRET_ACCESS_KEY")]
    [InlineData("ClientSecret")]
    [InlineData("SslCertFile")]
    [InlineData("SIGNATURE")]
    [InlineData("LicenseKey")]
    // Строка подключения сама по себе — учётные данные (пароль внутри).
    [InlineData("ApplicationInsights__ConnectionString__")]
    [InlineData("MarsSettings:ConnectionString")]
    public void IsSecretKey_WhenKeyLooksLikeCredential_ThenTrue(string key)
    {
        var result = SecretValueFilter.IsSecretKey(key);

        Assert.True(result);
    }

    [Theory]
    [InlineData("ASPNETCORE_ENVIRONMENT")]
    [InlineData("DOTNET_RUNNING_IN_CONTAINER")]
    [InlineData("PATH")]
    [InlineData("HOME")]
    [InlineData("OTEL_EXPORTER_OTLP_ENDPOINT")]
    [InlineData("MediaGit__RepositoryUrl")]
    [InlineData("MediaGit__Branch")]
    [InlineData("RabbitMq__Port")]
    [InlineData("OTEL_RESOURCE_ATTRIBUTES")]
    [InlineData("RabbitMq__Host")]
    [InlineData("Otlp__Endpoint")]
    public void IsSecretKey_WhenKeyIsNotCredential_ThenFalse(string key)
    {
        var result = SecretValueFilter.IsSecretKey(key);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSecretKey_WhenKeyIsEmpty_ThenFalse(string? key)
    {
        var result = SecretValueFilter.IsSecretKey(key);

        Assert.False(result);
    }

    [Fact]
    public void Filter_WhenSourceContainsSecrets_ThenReturnsOnlyNonSecretKeys()
    {
        var source = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ServiceAuth__ApiKey"] = "super-secret",
            ["POSTGRES_PASSWORD"] = "postgres",
            ["Loki__Url"] = "http://loki:3100",
        };

        var result = SecretValueFilter.Filter(source);

        Assert.Equal(["ASPNETCORE_ENVIRONMENT", "Loki__Url"], result.Select(item => item.Key));
    }

    [Fact]
    public void Filter_WhenSourceIsEmpty_ThenReturnsEmpty()
    {
        var result = SecretValueFilter.Filter([]);

        Assert.Empty(result);
    }
}
