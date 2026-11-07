using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Runtime;
using Wolverine.Util;

namespace MARS.Shared.Tests.Messaging;

public sealed class MarsMessagingInfrastructureTests
{
    [Fact]
    public void WolverineIsNotRegisteredWithoutRabbitMqHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = string.Empty;

        builder.AddMarsWolverine("MARS.Probe", typeof(IMarsEventMessage).Assembly);

        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IWolverineRuntime)
        );
    }

    [Fact]
    public async Task AddMarsWolverineRegistersNoOpPublisherWithoutRabbitMq()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = string.Empty;

        builder.AddMarsWolverine("MARS.Probe", typeof(IMarsEventMessage).Assembly);

        using var provider = builder.Services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMarsWolverineEventBus>();

        await publisher.PublishAsync(new ChatEventMessageEvent());
    }

    [Fact]
    public void WolverineRegistersWithMarkerBasedHandlerDiscovery()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = "localhost";

        builder.AddMarsWolverine("MARS.Probe", typeof(MarsMessagingInfrastructureTests).Assembly);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.Equal("MARS.Probe", options.ServiceName);
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(ProbeListener)));
        Assert.Contains("MISS", options.DescribeHandlerMatch(typeof(ProbeHandler)));
    }

    [Fact]
    public void QueueNameUsesServiceMessageTypeAndListenerName()
    {
        var queue = MarsQueues.QueueNameForListener(typeof(RewardAlertListener), "MARS.Alerts");

        Assert.Equal("mars.alerts.reward-redeemed.reward-alert", queue);
    }

    [Fact]
    public void QueueNameNormalizesPascalCaseServiceAndMessageType()
    {
        var queue = MarsQueues.QueueNameForListener(typeof(TwitchMediaListener), "MARS.TwitchCore");

        Assert.Equal("mars.twitch-core.chat-event-message.twitch-media", queue);
    }

    [Fact]
    public void QueueNameRequiresExactlyOneMessageListenerContract()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(ListenerWithoutMessageBase), "MARS.Alerts")
        );

        Assert.Contains(
            "must implement exactly one IMarsMessageListener<TMessage>",
            exception.Message
        );
        Assert.Contains("found 0", exception.Message);
    }

    [Fact]
    public void QueueNameRejectsTypesThatAreNotListeners()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(NotAListener), "MARS.Alerts")
        );

        Assert.Contains("found 0", exception.Message);
    }

    [Fact]
    public void QueueNameRejectsOpenGenericListenerTypes()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(OpenGenericListener<>), "MARS.Alerts")
        );

        Assert.Contains("concrete, closed listener class", exception.Message);
    }

    [Fact]
    public void QueueNameRejectsServiceNameThatNormalizesToEmpty()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(RewardAlertListener), "MARS.")
        );

        Assert.Contains("name segment cannot be empty", exception.Message);
    }

    [Fact]
    public void QueueNameRejectsListenerNameThatNormalizesToEmpty()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(Listener), "MARS.Alerts")
        );

        Assert.Contains("name segment cannot be empty", exception.Message);
    }

    [Fact]
    public void DeadLetterQueueKeepsExistingServiceNamingConvention()
    {
        Assert.Equal("mars.alerts.dlq", MarsQueues.DeadLetterQueueForService("MARS.Alerts"));
        Assert.Equal(
            "mars.twitchcore.dlq",
            MarsQueues.DeadLetterQueueForService("MARS.TwitchCore")
        );
    }

    [Fact]
    public void MessageExchangeNameComesFromClrTypeName()
    {
        Assert.Equal(
            "mars.reward-redeemed",
            MarsMessageNaming.ExchangeNameFor(typeof(RewardRedeemedEvent))
        );
        Assert.Equal(
            "mars.chat-event-message",
            MarsMessageNaming.ExchangeNameFor(typeof(ChatEventMessageEvent))
        );
        Assert.Equal(
            "mars.event-sub-raw",
            MarsMessageNaming.ExchangeNameFor(typeof(EventSubRawEvent))
        );
    }

    [Fact]
    public void MessageExchangeNamingRequiresMarkerInterface()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsMessageNaming.ExchangeNameFor(typeof(UnmarkedMessage))
        );

        Assert.Contains("must implement IMarsEventMessage", exception.Message);
    }

    [Fact]
    public void MessageExchangeNamingRejectsAbstractMessageTypes()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsMessageNaming.ExchangeNameFor(typeof(AbstractMessage))
        );

        Assert.Contains("must be a concrete, closed message class", exception.Message);
    }

    [Fact]
    public void MessageExchangeNamingRejectsOpenGenericMessageTypes()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsMessageNaming.ExchangeNameFor(typeof(OpenGenericMessage<>))
        );

        Assert.Contains("must be a concrete, closed message class", exception.Message);
    }

    [Fact]
    public void PublishingDiscoveryFindsOnlyMarkedConcreteMessageTypes()
    {
        var contracts = WolverineMessagingExtensions.DiscoverMessageContracts([
            typeof(RewardRedeemedEvent),
            typeof(ChatEventMessageEvent),
            typeof(UnmarkedMessage),
            typeof(AbstractMessage),
        ]);

        Assert.Collection(
            contracts.OrderBy(contract => contract.ExchangeName),
            contract =>
            {
                Assert.Equal(typeof(ChatEventMessageEvent), contract.Type);
                Assert.Equal("mars.chat-event-message", contract.ExchangeName);
            },
            contract =>
            {
                Assert.Equal(typeof(RewardRedeemedEvent), contract.Type);
                Assert.Equal("mars.reward-redeemed", contract.ExchangeName);
            }
        );
    }

    [Fact]
    public void SharedAssemblyExposesUniqueMarkedMessageContracts()
    {
        var contracts = WolverineMessagingExtensions.DiscoverMessageContracts(
            typeof(IMarsEventMessage).Assembly.GetTypes()
        );

        var exchangeNames = contracts.Select(contract => contract.ExchangeName).ToArray();

        Assert.Equal(6, contracts.Count);
        Assert.Equal(
            new[]
            {
                "mars.chat-event-message",
                "mars.chat-send",
                "mars.event-sub-raw",
                "mars.reward-redeemed",
                "mars.twitch-message-deleted",
                "mars.twitch-reward-input-message",
            },
            exchangeNames.OrderBy(name => name, StringComparer.Ordinal)
        );

        Assert.Contains(contracts, contract => contract.Type == typeof(RewardRedeemedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatEventMessageEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatSendEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(EventSubRawEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(TwitchMessageDeletedEvent));
        Assert.Contains(
            contracts,
            contract => contract.Type == typeof(TwitchRewardInputMessageEvent)
        );
    }

    [Fact]
    public void PublishingDiscoveryRejectsDuplicateExchangeNames()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WolverineMessagingExtensions.DiscoverMessageContracts([
                typeof(DuplicateOne.DuplicateEvent),
                typeof(DuplicateTwo.DuplicateEvent),
            ])
        );

        Assert.Contains("Duplicate RabbitMQ message exchange names detected", exception.Message);
        Assert.Contains("mars.duplicate", exception.Message);
    }

    [Fact]
    public void PublishingDiscoveryRejectsATypeNamedOnlyEvent()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WolverineMessagingExtensions.DiscoverMessageContracts([typeof(Event)])
        );

        Assert.Contains("message type name cannot be empty", exception.Message);
    }

    private sealed class ProbeMessage : IMarsEventMessage { }

    private sealed class ProbeListener : MarsEventListener<ProbeMessage>
    {
        public override Task Handle(ProbeMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ProbeHandler
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class RewardAlertListener : MarsEventListener<RewardRedeemedEvent>
    {
        public override Task Handle(
            RewardRedeemedEvent message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class TwitchMediaListener : MarsEventListener<ChatEventMessageEvent>
    {
        public override Task Handle(
            ChatEventMessageEvent message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class Listener : MarsEventListener<RewardRedeemedEvent>
    {
        public override Task Handle(
            RewardRedeemedEvent message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class ListenerWithoutMessageBase : IMarsEventListener { }

    private sealed class NotAListener { }

    private sealed class OpenGenericListener<TMessage> : MarsEventListener<TMessage>
        where TMessage : IMarsEventMessage
    {
        public override Task Handle(TMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class OpenGenericMessage<TMessage> : IMarsEventMessage { }

    private sealed class UnmarkedMessage { }

    private abstract class AbstractMessage : IMarsEventMessage { }

    private sealed class Event : IMarsEventMessage { }

    private static class DuplicateOne
    {
        public sealed class DuplicateEvent : IMarsEventMessage { }
    }

    private static class DuplicateTwo
    {
        public sealed class DuplicateEvent : IMarsEventMessage { }
    }
}
