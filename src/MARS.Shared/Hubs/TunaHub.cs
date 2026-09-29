using System.Net.WebSockets;
using MARS.Shared.Hubs.Models;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Shared.Hubs;

public class TunaHub : Hub
{
    private static ISingleClientProxy? _yandexMusicApplication;
    private static TunaMusicDTO? _lastState;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await Clients.Others.SendCoreAsync("TunaMusicInfo", [_lastState]);
    }

    public Task SendPlayerData(TunaMusicDTO info)
    {
        _lastState = info;
        return Clients.Others.SendCoreAsync("TunaMusicInfo", [_lastState]);
    }

    public Task BeYm()
    {
        if (_yandexMusicApplication != null)
        {
            throw new WebSocketException();
        }

        _yandexMusicApplication = Clients.Caller;
        return Task.CompletedTask;
    }
}
