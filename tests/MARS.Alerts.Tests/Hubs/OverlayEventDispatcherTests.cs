using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Диспетчер должен обслуживать каждую ветку сообщения без отдельного теста
/// на ветку: список веток берётся из рефлексии, а не из захардкоженного набора.
/// </summary>
public class OverlayEventDispatcherTests
{
    [Fact]
    public async Task Dispatch_routes_every_branch_to_the_matching_hub_method()
    {
        var branches = TelegramusEventContract.Branches;
        Assert.NotEmpty(branches);

        foreach (var branch in branches)
        {
            var clients = new Mock<IAlertsHub>();
            var context = CreateContext(clients);
            var payload = Activator.CreateInstance(branch.PropertyType);
            var notification = new TelegramusEvent();
            branch.SetValue(notification, payload);

            await OverlayEventDispatcher.DispatchAsync(
                context,
                notification,
                TestContext.Current.CancellationToken
            );

            var invocation = Assert.Single(clients.Invocations);
            Assert.Equal(branch.Name, invocation.Method.Name);
            Assert.Equal(invocation.Method.GetParameters().Length, invocation.Arguments.Count);
            if (invocation.Arguments.Count == 1)
            {
                Assert.Same(payload, invocation.Arguments[0]);
            }
        }
    }

    [Fact]
    public void EventCase_is_the_name_of_the_active_branch()
    {
        Assert.Null(new TelegramusEvent().EventCase);
        Assert.Equal("Alert", new TelegramusEvent { Alert = new AlertEvent() }.EventCase);
        Assert.Equal("Credits", new TelegramusEvent { Credits = new EmptyEvent() }.EventCase);
    }

    [Fact]
    public async Task Dispatch_ignores_event_without_case()
    {
        var clients = new Mock<IAlertsHub>();
        var context = CreateContext(clients);

        await OverlayEventDispatcher.DispatchAsync(
            context,
            new TelegramusEvent(),
            TestContext.Current.CancellationToken
        );

        clients.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Dispatch_throws_when_cancelled()
    {
        var clients = new Mock<IAlertsHub>();
        var context = CreateContext(clients);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            OverlayEventDispatcher.DispatchAsync(
                context,
                new TelegramusEvent { Credits = new EmptyEvent() },
                cts.Token
            )
        );

        clients.VerifyNoOtherCalls();
    }

    private static IHubContext<AlertsHub, IAlertsHub> CreateContext(Mock<IAlertsHub> clients)
    {
        var hubClients = new Mock<IHubClients<IAlertsHub>>();
        hubClients.Setup(c => c.All).Returns(clients.Object);

        var context = new Mock<IHubContext<AlertsHub, IAlertsHub>>();
        context.Setup(c => c.Clients).Returns(hubClients.Object);
        return context.Object;
    }
}
