using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Tests.Messaging;

/// <summary>
/// Параметры и фабрика AMQP-соединений.
///
/// Пароль не хранится в конфигурации: там только путь к файлу-секрету. Если бы
/// фабрика брала учётные данные из дефолтов RabbitMQ.Client, compose-стек
/// подключался бы как <c>guest</c>, а брокер его отверг.
/// </summary>
public class RabbitMqConnectionFactoryTests
{
    [Fact]
    public void OptionsAreReadFromSection()
    {
        var configuration = Configuration(
            new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = "rabbit",
                ["RabbitMq:Port"] = "5673",
                ["RabbitMq:UserName"] = "mars",
                ["RabbitMq:VirtualHost"] = "/mars",
                ["RabbitMq:ReconnectDelayMilliseconds"] = "1500",
            }
        );

        var options = RabbitMqConnectionFactory.CreateOptions(configuration);

        Assert.Equal("rabbit", options.Host);
        Assert.Equal(5673, options.Port);
        Assert.Equal("mars", options.UserName);
        Assert.Equal("/mars", options.VirtualHost);
        Assert.Equal(1500, options.ReconnectDelayMilliseconds);
    }

    [Fact]
    public void MissingSectionFallsBackToDefaults()
    {
        var options = RabbitMqConnectionFactory.CreateOptions(Configuration([]));

        Assert.Equal(RabbitMqOptions.SectionName, "RabbitMq");
        Assert.False(string.IsNullOrWhiteSpace(options.Host));
    }

    [Fact]
    public void PasswordIsReadFromSecretFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rabbit-secret-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "пароль-из-секрета\r\n");

        try
        {
            var options = RabbitMqConnectionFactory.CreateOptions(
                Configuration(new Dictionary<string, string?> { ["RabbitMq:Password_FILE"] = path })
            );

            Assert.Equal("пароль-из-секрета", options.Password);
            Assert.Equal(path, options.PasswordFile);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CamelCaseSecretFileIsAlsoSupported()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rabbit-camel-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "секрет");

        try
        {
            var options = RabbitMqConnectionFactory.CreateOptions(
                Configuration(new Dictionary<string, string?> { ["RabbitMq:PasswordFile"] = path })
            );

            Assert.Equal("секрет", options.Password);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Отсутствующий файл-секрет обязан быть виден сразу: иначе сервис стартовал бы
    /// и падал бы на первой попытке подключения без понятной причины.
    /// </summary>
    [Fact]
    public void MissingSecretFileIsReported()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rabbit-missing-{Guid.NewGuid():N}.txt");

        Assert.Throws<FileNotFoundException>(() =>
            RabbitMqConnectionFactory.CreateOptions(
                Configuration(new Dictionary<string, string?> { ["RabbitMq:Password_FILE"] = path })
            )
        );
    }

    [Fact]
    public void ConnectionFactoryKeepsCredentialsAndEnablesRecovery()
    {
        var factory = RabbitMqConnectionFactory.Create(
            new RabbitMqOptions
            {
                Host = "rabbit",
                Port = 5673,
                UserName = "mars",
                Password = "секрет",
                VirtualHost = "/mars",
                ReconnectDelayMilliseconds = 1500,
            },
            "mars-test-publisher"
        );

        Assert.Equal("rabbit", factory.HostName);
        Assert.Equal(5673, factory.Port);
        Assert.Equal("mars", factory.UserName);
        Assert.Equal("секрет", factory.Password);
        Assert.Equal("/mars", factory.VirtualHost);
        Assert.Equal("mars-test-publisher", factory.ClientProvidedName);
        Assert.True(factory.AutomaticRecoveryEnabled);
        Assert.True(factory.TopologyRecoveryEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), factory.NetworkRecoveryInterval);
    }

    [Fact]
    public void RecoveryIntervalNeverFallsBelowHalfSecond()
    {
        var factory = RabbitMqConnectionFactory.Create(
            new RabbitMqOptions { ReconnectDelayMilliseconds = 10 },
            "mars-test-publisher"
        );

        Assert.Equal(TimeSpan.FromMilliseconds(500), factory.NetworkRecoveryInterval);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
