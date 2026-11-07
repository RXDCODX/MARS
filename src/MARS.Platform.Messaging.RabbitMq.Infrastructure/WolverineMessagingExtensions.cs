using System.Reflection;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Transports;

namespace MARS.Platform.Messaging.RabbitMq.Infrastructure;

/// <summary>
/// Регистрация Wolverine поверх RabbitMQ и автоматическое построение маршрутов
/// по типизированным Listener и сообщениям, реализующим IMarsEventMessage.
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
            discovery.Includes.Implements<IMarsEventListener<IMarsEventMessage>>()
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

                    // Wolverine по умолчанию привязывает листенер к exchange с полным
                    // именем CLR-типа, а ConfigurePublishing публикует в mars.{message}.
                    // Без этой строки публикация и подписка расходятся по разным exchange.
                    convention.ExchangeNameForSending(MarsMessageNaming.ExchangeNameFor);
                }
            );

        ConfigurePublishing(options, typeof(IMarsEventMessage).Assembly);
    }

    /// <summary>
    /// Регистрирует исходящий маршрут для каждого конкретного класса-контракта
    /// из сборки MARS.Shared, реализующего IMarsEventMessage.
    /// </summary>
    private static void ConfigurePublishing(WolverineOptions options, Assembly contractsAssembly)
    {
        var contracts = DiscoverMessageContracts(contractsAssembly.GetTypes());

        foreach (var contract in contracts)
        {
            options.PublishMessage(contract.Type).ToRabbitExchange(contract.ExchangeName);
        }
    }

    internal static IReadOnlyList<MessageContract> DiscoverMessageContracts(
        IEnumerable<Type> contractTypes
    )
    {
        ArgumentNullException.ThrowIfNull(contractTypes);

        var contracts = contractTypes
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IMarsEventMessage).IsAssignableFrom(type)
            )
            .Select(type => new MessageContract(type, MarsMessageNaming.ExchangeNameFor(type)))
            .ToArray();

        var duplicateExchangeNames = contracts
            .GroupBy(contract => contract.ExchangeName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateExchangeNames.Length > 0)
        {
            throw new InvalidOperationException(
                "Duplicate RabbitMQ message exchange names detected: "
                    + string.Join(", ", duplicateExchangeNames)
            );
        }

        return contracts;
    }

    internal sealed record MessageContract(Type Type, string ExchangeName);
}
