#nullable disable
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Presentation.Overlay.Hubs;

/// <summary>
/// Раскладывает <see cref="TelegramusEvent"/> по типизированным методам хаба.
/// Заменяет удалённый <c>HubEventRelay</c>: вызовы идут напрямую в
/// <see cref="IHubContext{THub,T}"/>, без очереди broadcaster'а.
/// </summary>
public static class OverlayEventDispatcher
{
    public static async Task DispatchAsync(
        IHubContext<AlertsHub, IAlertsHub> hubContext,
        TelegramusEvent notification,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (notification.EventCase)
        {
            case TelegramusEvent.EventOneofCase.Alert:
                await hubContext.Clients.All.Alert(notification.Alert);
                break;
            case TelegramusEvent.EventOneofCase.Alerts:
                await hubContext.Clients.All.Alerts(notification.Alerts);
                break;
            case TelegramusEvent.EventOneofCase.WaifuRoll:
                await hubContext.Clients.All.WaifuRoll(notification.WaifuRoll);
                break;
            case TelegramusEvent.EventOneofCase.AddNewWaifu:
                await hubContext.Clients.All.AddNewWaifu(notification.AddNewWaifu);
                break;
            case TelegramusEvent.EventOneofCase.ShowCurrentWife:
                await hubContext.Clients.All.ShowCurrentWife(notification.ShowCurrentWife);
                break;
            case TelegramusEvent.EventOneofCase.MergeWaifu:
                await hubContext.Clients.All.MergeWaifu(notification.MergeWaifu);
                break;
            case TelegramusEvent.EventOneofCase.UpdateWaifuPrizes:
                await hubContext.Clients.All.UpdateWaifuPrizes(notification.UpdateWaifuPrizes);
                break;
            case TelegramusEvent.EventOneofCase.FumoFriday:
                await hubContext.Clients.All.FumoFriday(notification.FumoFriday);
                break;
            case TelegramusEvent.EventOneofCase.NewMessage:
                await hubContext.Clients.All.NewMessage(notification.NewMessage);
                break;
            case TelegramusEvent.EventOneofCase.DeleteMessage:
                await hubContext.Clients.All.DeleteMessage(notification.DeleteMessage);
                break;
            case TelegramusEvent.EventOneofCase.Highlite:
                await hubContext.Clients.All.Highlite(notification.Highlite);
                break;
            case TelegramusEvent.EventOneofCase.PostTwitchInfo:
                await hubContext.Clients.All.PostTwitchInfo(notification.PostTwitchInfo);
                break;
            case TelegramusEvent.EventOneofCase.MakeScreenParticles:
                await hubContext.Clients.All.MakeScreenParticles(notification.MakeScreenParticles);
                break;
            case TelegramusEvent.EventOneofCase.MakeScreenEmojisParticles:
                await hubContext.Clients.All.MakeScreenEmojisParticles(
                    notification.MakeScreenEmojisParticles
                );
                break;
            case TelegramusEvent.EventOneofCase.RandomMem:
                await hubContext.Clients.All.RandomMem(notification.RandomMem);
                break;
            case TelegramusEvent.EventOneofCase.AutoMessage:
                await hubContext.Clients.All.AutoMessage(notification.AutoMessage);
                break;
            case TelegramusEvent.EventOneofCase.Adhd:
                await hubContext.Clients.All.Adhd(notification.Adhd);
                break;
            case TelegramusEvent.EventOneofCase.Explosion:
                await hubContext.Clients.All.Explosion();
                break;
            case TelegramusEvent.EventOneofCase.LeroyAlert:
                await hubContext.Clients.All.LeroyAlert();
                break;
            case TelegramusEvent.EventOneofCase.GaoAlert:
                await hubContext.Clients.All.GaoAlert(notification.GaoAlert);
                break;
            case TelegramusEvent.EventOneofCase.Credits:
                await hubContext.Clients.All.Credits();
                break;
            case TelegramusEvent.EventOneofCase.MichaelJackson:
                await hubContext.Clients.All.MichaelJackson();
                break;
            case TelegramusEvent.EventOneofCase.MikuMonday:
                await hubContext.Clients.All.MikuMonday(notification.MikuMonday);
                break;
            case TelegramusEvent.EventOneofCase.MikuMikuBeam:
                await hubContext.Clients.All.MikuMikuBeam(notification.MikuMikuBeam);
                break;
            case TelegramusEvent.EventOneofCase.PhonkEdit:
                await hubContext.Clients.All.PhonkEdit();
                break;
            case TelegramusEvent.EventOneofCase.TikTokEdit:
                await hubContext.Clients.All.TikTokEdit(notification.TikTokEdit);
                break;
            case TelegramusEvent.EventOneofCase.AllRefund:
                await hubContext.Clients.All.AllRefund(notification.AllRefund);
                break;
            case TelegramusEvent.EventOneofCase.AudioQuizStart:
                await hubContext.Clients.All.AudioQuizStart(notification.AudioQuizStart);
                break;
            case TelegramusEvent.EventOneofCase.AudioQuizStop:
                await hubContext.Clients.All.AudioQuizStop();
                break;
            case TelegramusEvent.EventOneofCase.FumoRoll:
                await hubContext.Clients.All.FumoRoll(notification.FumoRoll);
                break;
            case TelegramusEvent.EventOneofCase.UpdateFumoPrizes:
                await hubContext.Clients.All.UpdateFumoPrizes(notification.UpdateFumoPrizes);
                break;
            case TelegramusEvent.EventOneofCase.FrogRoll:
                await hubContext.Clients.All.FrogRoll(notification.FrogRoll);
                break;
            case TelegramusEvent.EventOneofCase.UpdateFrogPrizes:
                await hubContext.Clients.All.UpdateFrogPrizes(notification.UpdateFrogPrizes);
                break;
            case TelegramusEvent.EventOneofCase.MikuRoll:
                await hubContext.Clients.All.MikuRoll(notification.MikuRoll);
                break;
            case TelegramusEvent.EventOneofCase.UpdateMikuPrizes:
                await hubContext.Clients.All.UpdateMikuPrizes(notification.UpdateMikuPrizes);
                break;
            case TelegramusEvent.EventOneofCase.AdhdConfig:
                await hubContext.Clients.All.AdhdConfig(notification.AdhdConfig);
                break;
            default:
                break;
        }
    }
}
