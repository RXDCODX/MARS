using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Универсальная заглушка оверлея: типизированный хаб.
/// </summary>
public sealed class TestOverlayHub : IHubContext<AlertsHub, IAlertsHub>, IHubContext
{
    private readonly Mock<IHubClients<IAlertsHub>> _typedClients = new();
    private readonly Mock<IHubClients> _plainClients = new();

    public TestOverlayHub()
    {
        _typedClients.Setup(c => c.All).Returns(ClientsMock.Object);
        Groups = new Mock<IGroupManager>().Object;
    }

    public Mock<IAlertsHub> ClientsMock { get; } = new();

    public IHubContext<AlertsHub, IAlertsHub> Context => this;

    public IHubClients<IAlertsHub> Clients => _typedClients.Object;

    IHubClients IHubContext.Clients => _plainClients.Object;

    public IGroupManager Groups { get; }
}
