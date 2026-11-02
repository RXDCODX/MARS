using Grpc.Net.Client;
using MARS.Shared.Grpc.EventSub;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Tests.Grpc;

public sealed class EventSubSubscriptionClientTests : IAsyncLifetime
{
    private readonly RecordingEventSubService _service = new();
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private IEventSubSubscriptionClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_service);

        _app = builder.Build();
        _app.MapGrpcService<RecordingEventSubService>();

        await _app.StartAsync(TestContext.Current.CancellationToken);

        _channel = GrpcChannel.ForAddress(
            "http://eventsub.test",
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() }
        );
        _client = new EventSubSubscriptionClient(
            new EventSubSubscriptionService.EventSubSubscriptionServiceClient(_channel)
        );
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ForwardsConditionAndTransport()
    {
        var request = new CreateEventSubSubscriptionRequest
        {
            Type = "channel.follow",
            Version = "2",
            RequestedBy = "MARS.Commands",
            Transport = new EventSubTransportInfo
            {
                Method = EventSubTransportMethodKind.Websocket,
                SessionId = "session-123",
            },
        };
        request.Condition.Add("broadcaster_user_id", "42");
        request.Condition.Add("moderator_user_id", "42");

        var reply = await _client.CreateSubscriptionAsync(
            request,
            TestContext.Current.CancellationToken
        );

        Assert.True(reply.Created);
        Assert.Equal("subscription-1", reply.Subscription.Id);
        Assert.NotNull(_service.CreateRequest);
        Assert.Equal("channel.follow", _service.CreateRequest.Type);
        Assert.Equal("2", _service.CreateRequest.Version);
        Assert.Equal("MARS.Commands", _service.CreateRequest.RequestedBy);
        Assert.Equal("session-123", _service.CreateRequest.Transport.SessionId);
        Assert.Equal("42", _service.CreateRequest.Condition["broadcaster_user_id"]);
        Assert.Equal("42", _service.CreateRequest.Condition["moderator_user_id"]);
    }

    [Fact]
    public async Task ListSubscriptionsAsync_ForwardsTypeFilter()
    {
        var reply = await _client.ListSubscriptionsAsync(
            new ListEventSubSubscriptionsRequest { Type = "stream.online" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("stream.online", _service.ListRequest?.Type);
        Assert.Single(reply.Subscriptions);
        Assert.Equal("stream.online", reply.Subscriptions[0].Type);
        Assert.Equal(1, reply.TotalCost);
        Assert.Equal(1000, reply.MaxTotalCost);
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_ForwardsSubscriptionId()
    {
        var reply = await _client.DeleteSubscriptionAsync(
            new DeleteEventSubSubscriptionRequest { Id = "subscription-123" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("subscription-123", _service.DeleteRequest?.Id);
        Assert.Equal("subscription-123", reply.Id);
    }

    private sealed class RecordingEventSubService
        : EventSubSubscriptionService.EventSubSubscriptionServiceBase
    {
        public CreateEventSubSubscriptionRequest? CreateRequest { get; private set; }
        public ListEventSubSubscriptionsRequest? ListRequest { get; private set; }
        public DeleteEventSubSubscriptionRequest? DeleteRequest { get; private set; }

        public override Task<CreateEventSubSubscriptionReply> CreateSubscription(
            CreateEventSubSubscriptionRequest request,
            global::Grpc.Core.ServerCallContext context
        )
        {
            CreateRequest = request;

            return Task.FromResult(
                new CreateEventSubSubscriptionReply
                {
                    Created = true,
                    Subscription = new EventSubSubscriptionInfo
                    {
                        Id = "subscription-1",
                        Type = request.Type,
                        Version = request.Version,
                    },
                }
            );
        }

        public override Task<ListEventSubSubscriptionsReply> ListSubscriptions(
            ListEventSubSubscriptionsRequest request,
            global::Grpc.Core.ServerCallContext context
        )
        {
            ListRequest = request;
            var reply = new ListEventSubSubscriptionsReply { TotalCost = 1, MaxTotalCost = 1000 };
            reply.Subscriptions.Add(
                new EventSubSubscriptionInfo
                {
                    Id = "subscription-2",
                    Type = request.Type,
                    Status = "enabled",
                }
            );

            return Task.FromResult(reply);
        }

        public override Task<DeleteEventSubSubscriptionReply> DeleteSubscription(
            DeleteEventSubSubscriptionRequest request,
            global::Grpc.Core.ServerCallContext context
        )
        {
            DeleteRequest = request;

            return Task.FromResult(new DeleteEventSubSubscriptionReply { Id = request.Id });
        }
    }
}
