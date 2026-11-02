using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Extensions;

/// <summary>
/// Registration helpers for the single application-level Twitch EventSub RabbitMQ consumer.
/// </summary>
public static class TwitchEventSubServiceCollectionExtensions
{
    /// <summary>
    /// Registers one Twitch EventSub consumer singleton and starts it as a hosted service.
    /// Call after <c>AddMarsDefaults</c>, which provides RabbitMQ connection options.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="serviceName">Stable application name used in RabbitMQ diagnostics.</param>
    /// <param name="queueName">
    /// Stable queue name unique to this application. If omitted, a name is derived from
    /// <paramref name="serviceName"/>. Replicas of the same application should share this queue.
    /// </param>
    public static IServiceCollection AddTwitchEventSubConsumer(
        this IServiceCollection services,
        string serviceName,
        string? queueName = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var actualQueueName = string.IsNullOrWhiteSpace(queueName)
            ? $"{serviceName.Trim().ToLowerInvariant()}.twitch.eventsub"
            : queueName.Trim();

        services.AddSingleton(sp =>
            new TwitchEventSubConsumer(
                sp.GetRequiredService<IOptions<RabbitMqOptions>>(),
                serviceName.Trim(),
                actualQueueName,
                sp.GetRequiredService<ILogger<TwitchEventSubConsumer>>()
            )
        );
        services.AddHostedService(sp => sp.GetRequiredService<TwitchEventSubConsumer>());

        return services;
    }
}
