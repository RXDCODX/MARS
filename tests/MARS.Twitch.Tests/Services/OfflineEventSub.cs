using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Client;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Клиент EventSub без сети.
///
/// У <c>EventSubWebsocketClient</c> есть конструктор с готовым сокетом, а сам
/// <c>WebsocketClient</c> подключается только в <c>ConnectAsync</c>. То есть
/// зависимость собирается ровно как в проде, но в тесте не открывает ни одного
/// соединения: иначе обработчики наград и фоловеров нельзя было бы проверять
/// вовсе.
/// </summary>
internal static class OfflineEventSub
{
    public static EventSubWebsocketClient Create()
    {
        var socket = new WebsocketClient(NullLogger<WebsocketClient>.Instance);
        var services = new ServiceCollection();
        services.AddSingleton(socket);

        return new EventSubWebsocketClient(
            NullLogger<EventSubWebsocketClient>.Instance,
            services.BuildServiceProvider(),
            socket
        );
    }
}
