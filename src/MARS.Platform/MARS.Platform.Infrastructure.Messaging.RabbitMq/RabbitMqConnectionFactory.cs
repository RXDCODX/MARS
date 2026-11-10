using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace MARS.Platform.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Единая фабрика AMQP-соединений для всех MARS-сервисов.
/// Гарантирует, что учётные данные берутся из конфигурации (в т.ч. из docker-secret),
/// а не из дефолтов RabbitMQ.Client, и что включено автоматическое восстановление соединения.
/// </summary>
public static class RabbitMqConnectionFactory
{
    public static RabbitMqOptions CreateOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(RabbitMqOptions.SectionName);
        var options = new RabbitMqOptions();
        section.Bind(options);

        var passwordFile = section["Password_FILE"] ?? section["PasswordFile"];

        if (!string.IsNullOrWhiteSpace(passwordFile))
        {
            options.PasswordFile = passwordFile;
            options.Password = ReadSecret(passwordFile);
        }

        return options;
    }

    public static ConnectionFactory Create(RabbitMqOptions options, string clientProvidedName)
    {
        var factory = new ConnectionFactory();
        Apply(factory, options, clientProvidedName);
        return factory;
    }

    /// <summary>
    /// Применяет общие настройки MARS к фабрике, созданной транспортом Wolverine.
    /// Параметры совпадают с фабрикой, которую возвращает <see cref="Create"/>.
    /// </summary>
    public static void Apply(
        ConnectionFactory factory,
        RabbitMqOptions options,
        string clientProvidedName
    )
    {
        factory.HostName = options.Host;
        factory.Port = options.Port;
        factory.UserName = options.UserName;
        factory.Password = options.Password;
        factory.VirtualHost = options.VirtualHost;
        factory.ClientProvidedName = clientProvidedName;
        factory.AutomaticRecoveryEnabled = true;
        factory.TopologyRecoveryEnabled = true;
        factory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        factory.NetworkRecoveryInterval = TimeSpan.FromMilliseconds(
            Math.Max(500, options.ReconnectDelayMilliseconds)
        );
        factory.ConsumerDispatchConcurrency = 1;
    }

    private static string ReadSecret(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"RabbitMq:Password_FILE указывает на '{path}', но файл не существует. "
                    + "Создайте secrets/rabbitmq_password.txt (см. secrets/README.md).",
                path
            );
        }

        return File.ReadAllText(path).TrimEnd('\r', '\n');
    }
}
