using MARS.Shared.Extensions;
using MARS.Shared.Grpc.EventSub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Tests.Grpc;

public class EventSubSubscriptionClientRegistrationTests
{
    [Fact]
    public void ClientRegistrationUsesTwitchCoreGrpcEndpoint()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ServiceEndpoints:TwitchCore"] = "http://twitch-core:8080",
                    ["Grpc:Endpoints:TwitchCore"] = "http://localhost:6034",
                }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMarsEventSubSubscriptionClient(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEventSubSubscriptionClient>());
        Assert.NotNull(
            provider.GetRequiredService<EventSubSubscriptionService.EventSubSubscriptionServiceClient>()
        );
    }
}
