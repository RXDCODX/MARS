using MARS.Alerts.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Hubs;

/// <summary>
/// Хаб оверлея. Единственная точка входа для браузера: всё, что раньше
/// доставлялось подпиской gRPC, приходит сюда тем же набором методов.
/// </summary>
/// <remarks>
/// <para>
/// Наследование от <see cref="Hub{T}"/> даёт <c>Clients.Client(...)</c>
/// типизированным: имя метода и его сигнатура берутся из
/// <see cref="IOverlayHub"/>, поэтому опечатка в строке невозможна в принципе.
/// </para>
/// <para>
/// Своего состояния у хаба нет намеренно. Набор подписчиков и очередь событий
/// живут в <c>GrpcEventBroadcaster</c>, откуда их читает <see cref="HubEventRelay"/>
/// на обслуживании хостов. Если бы подписки учитывались здесь, то при двух
/// подключённых оверлеях и перезапуске фоновой задачи состояние разъехалось
/// бы с тем, что видят клиенты.
/// </para>
/// </remarks>
public class OverlayHub : Hub<IOverlayHub>;
