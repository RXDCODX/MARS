using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace MARS.Shared.Messaging;

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
        return new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            ClientProvidedName = clientProvidedName,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(30),
            NetworkRecoveryInterval = TimeSpan.FromMilliseconds(
                Math.Max(500, options.ReconnectDelayMilliseconds)
            ),
            ConsumerDispatchConcurrency = 1,
        };
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
