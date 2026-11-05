using MARS.Shared.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Attributes;
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

        builder.AddMarsWolverine("MARS.Probe", typeof(MarsMessageAliases).Assembly);

        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IWolverineRuntime)
        );
    }

    [Fact]
    public void WolverineRegistersWithAutomaticRoutesWhenRabbitMqHostIsConfigured()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = "localhost";

        builder.AddMarsWolverine("MARS.Probe", typeof(MarsMessageAliases).Assembly);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.Equal("MARS.Probe", options.ServiceName);
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(ProbeListener)));
        Assert.Contains(
            "MISS -- Name ends with 'Listener'",
            options.DescribeHandlerMatch(typeof(ProbeHandler))
        );
    }

    [Fact]
    public void QueueNameUsesServiceMessageIdentityAndListenerName()
    {
        var queue = MarsQueues.QueueNameForListener(typeof(RewardAlertListener), "MARS.Alerts");

        Assert.Equal("mars.alerts.reward-redeemed.reward-alert", queue);
    }

    [Fact]
    public void QueueNameNormalizesPascalCaseServiceAndDottedMessageAlias()
    {
        var queue = MarsQueues.QueueNameForListener(typeof(TwitchMediaListener), "MARS.TwitchCore");

        Assert.Equal("mars.twitch-core.chat-message-received.twitch-media", queue);
    }

    [Fact]
    public void QueueNameRequiresExactlyOneGenericListenerContract()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(MultipleMessageListener), "MARS.Alerts")
        );

        Assert.Contains("must implement exactly one IMarsListener<TMessage>", exception.Message);
        Assert.Contains("found 2", exception.Message);
    }

    [Fact]
    public void QueueNameRejectsTypesThatDoNotImplementGenericListenerContract()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(NotAListener), "MARS.Alerts")
        );

        Assert.Contains("found 0", exception.Message);
    }

    [Fact]
    public void QueueNameRequiresMessageIdentity()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MarsQueues.QueueNameForListener(typeof(ListenerWithoutIdentity), "MARS.Alerts")
        );

        Assert.Contains("Missing MessageIdentity", exception.Message);
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
    public void PublishingDiscoveryFindsOnlyAttributedConcreteMessageTypes()
    {
        var contracts = WolverineMessagingExtensions.DiscoverMessageContracts([
            typeof(RewardRedeemedMessage),
            typeof(ChatMessageReceivedMessage),
            typeof(UnmarkedMessage),
            typeof(AbstractMessage),
        ]);

        Assert.Collection(
            contracts.OrderBy(contract => contract.Alias),
            contract =>
            {
                Assert.Equal(typeof(ChatMessageReceivedMessage), contract.Type);
                Assert.Equal("mars.chat.message.received", contract.Alias);
            },
            contract =>
            {
                Assert.Equal(typeof(RewardRedeemedMessage), contract.Type);
                Assert.Equal("mars.reward.redeemed", contract.Alias);
            }
        );
    }

    [Fact]
    public void SharedAssemblyExposesUniqueAttributedIntegrationContracts()
    {
        var contracts = WolverineMessagingExtensions.DiscoverMessageContracts(
            typeof(MarsMessageAliases).Assembly.GetTypes()
        );

        Assert.Equal(6, contracts.Count);
        Assert.Equal(
            contracts.Count,
            contracts.Select(contract => contract.Alias).Distinct(StringComparer.Ordinal).Count()
        );
        Assert.Contains(contracts, contract => contract.Type == typeof(RewardRedeemedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatSendEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatMessageReceivedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(RewardInputMessageEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(ChatMessageDeletedEvent));
        Assert.Contains(contracts, contract => contract.Type == typeof(EventSubRawEvent));
    }

    [Fact]
    public void PublishingDiscoveryRejectsDuplicateAliases()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WolverineMessagingExtensions.DiscoverMessageContracts([
                typeof(DuplicateAliasMessageOne),
                typeof(DuplicateAliasMessageTwo),
            ])
        );

        Assert.Contains("Duplicate RabbitMQ message aliases detected", exception.Message);
        Assert.Contains("mars.duplicate", exception.Message);
    }

    [Fact]
    public void PublishingDiscoveryRejectsEmptyAliases()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WolverineMessagingExtensions.DiscoverMessageContracts([typeof(EmptyAliasMessage)])
        );

        Assert.Contains("MessageIdentity alias is empty", exception.Message);
    }

    private sealed class ProbeListener
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class ProbeHandler
    {
        public static void Handle(string message) => _ = message;
    }

    private sealed class RewardAlertListener : IMarsListener<RewardRedeemedMessage>
    {
        public Task Handle(RewardRedeemedMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class TwitchMediaListener : IMarsListener<ChatMessageReceivedMessage>
    {
        public Task Handle(
            ChatMessageReceivedMessage message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class MultipleMessageListener
        : IMarsListener<RewardRedeemedMessage>,
            IMarsListener<ChatMessageReceivedMessage>
    {
        Task IMarsListener<RewardRedeemedMessage>.Handle(
            RewardRedeemedMessage message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;

        Task IMarsListener<ChatMessageReceivedMessage>.Handle(
            ChatMessageReceivedMessage message,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }

    private sealed class NotAListener { }

    private sealed class ListenerWithoutIdentity : IMarsListener<UnmarkedMessage>
    {
        public Task Handle(UnmarkedMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [MessageIdentity("mars.reward.redeemed")]
    private sealed class RewardRedeemedMessage { }

    [MessageIdentity("mars.chat.message.received")]
    private sealed class ChatMessageReceivedMessage { }

    private sealed class UnmarkedMessage { }

    private abstract class AbstractMessage { }

    [MessageIdentity("mars.duplicate")]
    private sealed class DuplicateAliasMessageOne { }

    [MessageIdentity("mars.duplicate")]
    private sealed class DuplicateAliasMessageTwo { }

    [MessageIdentity(" ")]
    private sealed class EmptyAliasMessage { }
}
