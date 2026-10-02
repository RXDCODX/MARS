using MARS.TwitchCore.Entities.Interfaces;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Hosting;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets;

namespace MARS.TwitchCore.Services.Rewards;

public class MiniGamesManager(
    IEnumerable<ITwitchMiniGame> miniGames,
    IHostApplicationLifetime lifetime,
    ITwitchClient client,
    EventSubWebsocketClient wsClient,
    ITwitchEventValidationService validator
) : IHostedService
{
    public bool IsServiceActive { get; set; } = true;

    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private readonly Dictionary<int, ITwitchMiniGame> _registeredGames = miniGames.ToDictionary(
        g => g.GetGameCost(),
        g => g
    );

    private readonly Dictionary<int, ITwitchMiniGame> _activeGames = [];

    public Task StartAsync(CancellationToken token)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            wsClient.ChannelPointsCustomRewardRedemptionAdd +=
                WsClientOnChannelPointsCustomRewardRedemptionAdd;
            client.OnMessageReceived += ClientOnMessageReceived;
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token)
    {
        _activeGames.Clear();
        wsClient.ChannelPointsCustomRewardRedemptionAdd -=
            WsClientOnChannelPointsCustomRewardRedemptionAdd;
        client.OnMessageReceived -= ClientOnMessageReceived;

        return Task.CompletedTask;
    }

    private async Task WsClientOnChannelPointsCustomRewardRedemptionAdd(
        object? sender,
        ChannelPointsCustomRewardRedemptionArgs args
    )
    {
        var cost = args.Payload.Event.Reward.Cost;

        if (!_registeredGames.ContainsKey(cost))
        {
            return;
        }

        var vr = await validator
            .ForRedemption(args)
            .RequireServiceActive(IsServiceActive)
            .ValidateWithResponseAsync(args.Payload.Event.UserName);

        if (vr.IsInvalid)
        {
            return;
        }

        var name = args.Payload.Event.UserName;
        var userId = args.Payload.Event.UserId;

        RemoveCompletedGames();

        var runningGame = _activeGames.Values.FirstOrDefault(e => e.IsGameRunning);
        if (runningGame is not null)
        {
            var activeGame = _activeGames[cost];
            if (!activeGame.IsReuseRewardForAddMechanic)
            {
                await client.SendMessageToMainTwitchAsync(
                    @$"@{name}, прости но уже другая игра происходит!"
                );
            }
            else
            {
                await activeGame.OnRewardRedemption(name, userId, cost);
            }
        }
        else if (_registeredGames.TryGetValue(cost, out var game))
        {
            game.IsGameRunning = true;
            _activeGames[cost] = game;
            await game.GameStart(name, userId, _cancellationToken);
        }
    }

    public async Task CancelAllGamesAsync()
    {
        foreach (var game in _activeGames.Values)
        {
            if (game.IsGameRunning)
            {
                await game.CancelAsync();
            }
        }
    }

    public void RemoveCompletedGames()
    {
        var completedGames = _activeGames.Where(kvp => !kvp.Value.IsGameRunning).ToList();
        foreach (var (key, _) in completedGames)
        {
            _activeGames.Remove(key);
        }
    }

    private async Task ClientOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var vr = await validator
            .ForMessageReceived(e)
            .RequireServiceActive(IsServiceActive)
            .SkipBlacklisted()
            .ValidateWithResponseAsync(e.ChatMessage.Username);

        if (vr.IsInvalid)
        {
            return;
        }

        RemoveCompletedGames();

        var userName = e.ChatMessage.Username;
        var message = e.ChatMessage.Message;
        var userId = e.ChatMessage.UserId;

        foreach (var game in _activeGames.Values.Where(game => game.IsGameRunning))
        {
            await game.OnChatMessage(userName, userId, message);
        }
    }
}
