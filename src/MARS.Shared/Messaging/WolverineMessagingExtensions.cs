using System.Reflection;
using MARS.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Transports;

namespace MARS.Shared.Messaging;

/// <summary>
/// Регистрация Wolverine поверх RabbitMQ и автоматическое построение маршрутов
/// по generic Listener-контрактам и MessageIdentity интеграционных сообщений.
/// </summary>
public static class WolverineMessagingExtensions
{
    /// <summary>
    /// Подключает шину для сервиса. Если адрес RabbitMQ не задан, Wolverine
    /// не регистрируется — сервисы без брокера продолжают запускаться автономно.
    /// </summary>
    public static WebApplicationBuilder AddMarsWolverine(
        this WebApplicationBuilder builder,
        string serviceName,
        Assembly applicationAssembly
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        var configuredHost = builder.Configuration[$"{RabbitMqOptions.SectionName}:Host"];

        if (!string.IsNullOrWhiteSpace(configuredHost))
        {
            var connection = RabbitMqConnectionFactory.CreateOptions(builder.Configuration);

            builder.Services.AddWolverine(options =>
            {
                options.ServiceName = serviceName;
                options.ApplicationAssembly = applicationAssembly;

                ConfigureDiscovery(options, applicationAssembly);
                ConfigureRabbitMq(options, serviceName, connection);
            });
        }

        return builder;
    }

    private static void ConfigureDiscovery(WolverineOptions options, Assembly applicationAssembly)
    {
        options.Discovery.DisableConventionalDiscovery();
        options.Discovery.CustomizeHandlerDiscovery(discovery =>
            discovery.Includes.WithNameSuffix("Listener")
        );
        options.Discovery.IncludeAssembly(applicationAssembly);
    }

    private static void ConfigureRabbitMq(
        WolverineOptions options,
        string serviceName,
        RabbitMqOptions connection
    )
    {
        options
            .UseRabbitMq(factory =>
                RabbitMqConnectionFactory.Apply(factory, connection, serviceName)
            )
            .AutoProvision()
            .EnableEnhancedDeadLettering()
            .CustomizeDeadLetterQueueing(
                new DeadLetterQueue(MarsQueues.DeadLetterQueueForService(serviceName))
            )
            .ConfigureChannelCreation(channel =>
            {
                channel.PublisherConfirmationsEnabled = true;
                channel.PublisherConfirmationTrackingEnabled = true;
            })
            .UseConventionalRouting(
                NamingSource.FromHandlerType,
                convention =>
                {
                    convention.OnlyApplyToInboundMessages();
                    convention.QueueNameForListener(listenerType =>
                        MarsQueues.QueueNameForListener(listenerType, serviceName)
                    );
                }
            );

        ConfigurePublishing(options, typeof(MarsMessageAliases).Assembly);
    }

    /// <summary>
    /// Регистрирует исходящий маршрут для каждого класса-контракта,
    /// помеченного Wolverine MessageIdentityAttribute.
    /// </summary>
    private static void ConfigurePublishing(WolverineOptions options, Assembly contractsAssembly)
    {
        var contracts = DiscoverMessageContracts(contractsAssembly.GetTypes());

        foreach (var contract in contracts)
        {
            options.PublishMessage(contract.Type).ToRabbitExchange(contract.Alias);
        }
    }

    internal static IReadOnlyList<MessageContract> DiscoverMessageContracts(
        IEnumerable<Type> contractTypes
    )
    {
        ArgumentNullException.ThrowIfNull(contractTypes);

        var contracts = contractTypes
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .Select(type => new
            {
                Type = type,
                Identity = type.GetCustomAttribute<Wolverine.Attributes.MessageIdentityAttribute>(),
            })
            .Where(candidate => candidate.Identity is not null)
            .Select(candidate =>
            {
                var alias = candidate.Identity!.Alias;

                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new InvalidOperationException(
                        $"MessageIdentity alias is empty for {candidate.Type.FullName ?? candidate.Type.Name}."
                    );
                }

                return new MessageContract(candidate.Type, alias);
            })
            .ToArray();

        var duplicateAliases = contracts
            .GroupBy(contract => contract.Alias, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateAliases.Length > 0)
        {
            throw new InvalidOperationException(
                "Duplicate RabbitMQ message aliases detected: "
                    + string.Join(", ", duplicateAliases)
            );
        }

        return contracts;
    }

    internal sealed record MessageContract(Type Type, string Alias);
}
