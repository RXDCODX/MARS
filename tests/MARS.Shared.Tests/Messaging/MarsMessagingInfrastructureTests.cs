using MARS.Shared.Messaging;
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

        builder.AddMarsWolverine("MARS.Probe", typeof(IMarsMessage).Assembly);

        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IWolverineRuntime)
        );
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

        Assert.Equal("mars.twitch-core.chat-message-received.twitch-media", queue);
    }

    [Fact]
    public void QueueNameRequiresExactlyOneClosedGenericListenerBase()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(ListenerWithoutMessageBase), "MARS.Alerts")
        );

        Assert.Contains("must inherit from exactly one MarsListener<TMessage>", exception.Message);
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

        Assert.Contains("must implement IMarsMessage", exception.Message);
    }

    [Fact]
    public void PublishingDiscoveryFindsOnlyMarkedConcreteMessageTypes()
    {
        var contracts = WolverineMessagingExtensions.DiscoverMessageContracts([
            typeof(RewardRedeemedEvent),
            typeof(ChatMessageReceivedEvent),
            typeof(UnmarkedMessage),
            typeof(AbstractMessage),
        ]);

        Assert.Collection(
            contracts.OrderBy(contract => contract.ExchangeName),
            contract =>
            {
                Assert.Equal(typeof(ChatMessageReceivedEvent), contract.Type);
                Assert.Equal("mars.chat-message-received", contract.ExchangeName);
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
            typeof(IMarsMessage).Assembly.GetTypes()
        );

        var exchangeNames = contracts.Select(contract => contract.ExchangeName).ToArray();

        Assert.Equal(7, contracts.Count);
        Assert.Equal(
            new[]
            {
                "mars.chat-message",
                "mars.chat-message-deleted",
                "mars.chat-message-received",
                "mars.chat-send",
                "mars.event-sub-raw",
                "mars.reward-input-message",
                "mars.reward-redeemed",
            },
            exchangeNames.OrderBy(name => name, StringComparer.Ordinal)
        );

        Assert.Contains(contracts, contract => contract.Type == typeof(RewardRedeemedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatMessageEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatSendEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatMessageReceivedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(RewardInputMessageEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatMessageDeletedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(EventSubRawEvent));
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

    private sealed class ProbeMessage : IMarsMessage { }

    private sealed class ProbeListener : MarsListener<ProbeMessage>
    {
        public override Task Handle(ProbeMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ProbeHandler
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class RewardAlertListener : MarsListener<RewardRedeemedEvent>
    {
        public override Task Handle(
            RewardRedeemedEvent message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class TwitchMediaListener : MarsListener<ChatMessageReceivedEvent>
    {
        public override Task Handle(
            ChatMessageReceivedEvent message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class ListenerWithoutMessageBase : IMarsListener { }

    private sealed class NotAListener { }

    private sealed class OpenGenericListener<TMessage> : MarsListener<TMessage>
        where TMessage : IMarsMessage
    {
        public override Task Handle(TMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class UnmarkedMessage { }

    private abstract class AbstractMessage : IMarsMessage { }

    private sealed class Event : IMarsMessage { }

    private static class DuplicateOne
    {
        public sealed class DuplicateEvent : IMarsMessage { }
    }

    private static class DuplicateTwo
    {
        public sealed class DuplicateEvent : IMarsMessage { }
    }
}
