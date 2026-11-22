using System.Threading.Channels;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Универсальная заглушка оверлея: одновременно типизированный хаб, порт
/// <see cref="IOverlayEventSink"/> и Entropy-совместимый broadcaster.
/// </summary>
public sealed class TestOverlayHub
    : IHubContext<AlertsHub, IAlertsHub>,
        IHubContext
{
    private readonly Mock<IHubClients<IAlertsHub>> _typedClients = new();
    private readonly Mock<IHubClients> _plainClients = new();
    private readonly Channel<TelegramusEvent> _events = Channel.CreateUnbounded<TelegramusEvent>();

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


    /// <summary>Совместимый с прежними тестами reader событий.</summary>
    public Subscription Subscribe() => new(this);

    public sealed class Subscription(TestOverlayHub owner) : IDisposable
    {
        public ChannelReader<TelegramusEvent> Reader => owner._events.Reader;

        public void Dispose() { }
    }
}
