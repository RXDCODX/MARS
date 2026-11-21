using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Диспетчер раскладывает <see cref="TelegramusEvent"/> по типизированным
/// методам хаба — замена удалённого HubEventRelay.
/// </summary>
public class OverlayEventDispatcherTests
{
    [Fact]
    public async Task Dispatch_routes_alert_case()
    {
        var clients = new Mock<IAlertsHub>();
        var context = CreateContext(clients);

        await OverlayEventDispatcher.DispatchAsync(
            context,
            new TelegramusEvent { Alert = new AlertEvent() },
            TestContext.Current.CancellationToken
        );

        clients.Verify(c => c.Alert(It.IsAny<AlertEvent>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_routes_empty_case_without_payload()
    {
        var clients = new Mock<IAlertsHub>();
        var context = CreateContext(clients);

        await OverlayEventDispatcher.DispatchAsync(
            context,
            new TelegramusEvent { Credits = new EmptyEvent() },
            TestContext.Current.CancellationToken
        );

        clients.Verify(c => c.Credits(), Times.Once);
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
