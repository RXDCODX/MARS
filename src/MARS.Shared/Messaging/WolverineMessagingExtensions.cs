using System.Reflection;
using MARS.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Transports;

namespace MARS.Shared.Messaging;

/// <summary>
/// Регистрация шины обмена Wolverine поверх RabbitMQ.
/// </summary>
/// <remarks>
/// <para>
/// Один обменник на тип сообщения (имя — псевдоним из
/// <see cref="MarsMessageAliases"/>), своя очередь на каждый слушатель и своя
/// dead-letter очередь на сервис. Общий обменник <c>mars.events</c> с ручными
/// routing key'ами больше не создаётся: маршрутизация теперь задаётся типом
/// сообщения, а не строкой, и опечатка в ключе не может молча увести событие в
/// очередь, которую никто не читает.
/// </para>
/// <para>
/// Обнаружение обработчиков включается только для имён с суффиксом
/// <c>Listener</c>. Иначе 33 reward-хендлера, у каждого из которых есть метод
/// <c>HandleAsync</c>, получили бы по собственной очереди и выполнялись бы все
/// на каждое погашение награды.
/// </para>
/// </remarks>
public static class WolverineMessagingExtensions
{
    /// <summary>
    /// Сообщения, которые уходят в брокер. Список явный: тип, который
    /// обрабатывается и локально, Wolverine иначе счёл бы локальным и не
    /// отправил бы в RabbitMQ вовсе.
    /// </summary>
    private static readonly (Type Type, string Alias)[] BrokerContracts =
    [
        (typeof(RewardRedeemedEvent), MarsMessageAliases.RewardRedeemed),
        (typeof(ChatMessageReceivedEvent), MarsMessageAliases.ChatMessageReceived),
        (typeof(RewardInputMessageEvent), MarsMessageAliases.RewardInputMessage),
        (typeof(ChatMessageDeletedEvent), MarsMessageAliases.ChatMessageDeleted),
        (typeof(ChatSendEvent), MarsMessageAliases.ChatSend),
        (typeof(EventSubRawEvent), MarsMessageAliases.EventSubRaw),
    ];

    /// <summary>
    /// Подключает шину к сервису.
    /// </summary>
    /// <param name="builder">Построитель приложения.</param>
    /// <param name="serviceName">Имя сервиса — оно попадает в имя DLQ и в телеметрию.</param>
    /// <param name="applicationAssembly">
    /// Сборка сервиса со слушателями. Передаётся явно (<c>typeof(Program).Assembly</c>),
    /// а не берётся из <see cref="Assembly.GetEntryAssembly"/>: иначе тест хоста
    /// обнаружил бы не те обработчики, а свои.
    /// </param>
    public static WebApplicationBuilder AddMarsWolverine(
        this WebApplicationBuilder builder,
        string serviceName,
        Assembly applicationAssembly
    )
    {
        // Регистрация зависит от наличия адреса брокера в конфигурации, а не от
        // значения по умолчанию в RabbitMqOptions: иначе сервис без секции
        // RabbitMq подключался бы к localhost и падал на старте.
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
                // Подтверждения публикации: без них «опубликовано» означает лишь
                // «записано в сокет», и потеря сообщения при обрыве соединения
                // осталась бы незамеченной.
                channel.PublisherConfirmationsEnabled = true;
                channel.PublisherConfirmationTrackingEnabled = true;
            })
            .UseConventionalRouting(
                NamingSource.FromHandlerType,
                convention =>
                {
                    // Маршрутизация «от слушателя» нужна ради отдельной очереди на
                    // каждого из них, а не ради отправки. Отправку задаёт явный
                    // список BrokerContracts ниже: иначе тип, обрабатываемый и
                    // локально, ушёл бы в локальную очередь вместо обменника.
                    convention.OnlyApplyToInboundMessages();
                    convention.QueueNameForListener(MarsQueues.QueueNameForHandlerType);
                }
            );

        foreach (var (type, alias) in BrokerContracts)
        {
            options.PublishMessage(type).ToRabbitExchange(alias);
        }
    }
}
