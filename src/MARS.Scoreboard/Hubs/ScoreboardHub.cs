using MARS.Scoreboard.Hubs.Interfaces;
using MARS.Scoreboard.Entities;
using MARS.Scoreboard.Services;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Scoreboard.Hubs;

public class ScoreboardHub(ScoreboardService scoreboardService, ILogger<ScoreboardHub> logger)
    : Hub<IScoreboardHub>
{
    public async Task JoinAsClient()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "client");
        logger.LogInformation("Client joined scoreboard hub: {ConnectionId}", Context.ConnectionId);
    }

    public async Task GetCurrentState()
    {
        var state = await scoreboardService.GetCurrentStateAsync();
        await Clients.Caller.ReceiveState(state);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.ReceiveState(await scoreboardService.GetCurrentStateAsync());
    }

    public async Task UpdateState(ScoreboardDto state)
    {
        await scoreboardService.UpdateStateAsync(state);
        await Clients.Others.StateUpdated(state);
        logger.LogInformation("Scoreboard state updated by {ConnectionId}", Context.ConnectionId);
    }

    public async Task UpdatePlayerScore(int playerPosition, int newScore)
    {
        var success = await scoreboardService.UpdatePlayerScoreAsync(playerPosition, newScore);
        if (success)
        {
            await Clients.Others.PlayerScoreUpdated(playerPosition, newScore);
            logger.LogInformation(
                "Player {Position} score updated to {Score} by {ConnectionId}",
                playerPosition,
                newScore,
                Context.ConnectionId
            );
        }
    }

    public async Task SetPlayerFinal(int playerPosition, string final)
    {
        var success = await scoreboardService.SetPlayerFinalAsync(playerPosition, final);
        if (success)
        {
            await Clients.Others.PlayerFinalUpdated(playerPosition, final);
            logger.LogInformation(
                "Player {Position} final status set to {Final} by {ConnectionId}",
                playerPosition,
                final,
                Context.ConnectionId
            );
        }
    }

    public async Task SetVisibility(bool isVisible)
    {
        var success = await scoreboardService.SetVisibilityAsync(isVisible);
        if (success)
        {
            await Clients.Others.VisibilityChanged(isVisible);
            logger.LogInformation(
                "Scoreboard visibility set to {IsVisible} by {ConnectionId}",
                isVisible,
                Context.ConnectionId
            );
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "Client disconnected from scoreboard hub: {ConnectionId}",
            Context.ConnectionId
        );
        await base.OnDisconnectedAsync(exception);
    }
}
