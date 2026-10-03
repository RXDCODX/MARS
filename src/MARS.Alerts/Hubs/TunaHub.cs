using MARS.Alerts.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Hubs;

/// <summary>
/// Хаб информации о треке.
/// </summary>
/// <remarks>
/// Устроен так же, как <see cref="OverlayHub"/>: своего состояния нет, всё
/// приходит из широковещателя через реле. Отдельный хаб нужен потому, что у
/// события трека своя подписка и своя форма — см. <see cref="ITunaHub"/>.
/// </remarks>
public class TunaHub : Hub<ITunaHub>;
