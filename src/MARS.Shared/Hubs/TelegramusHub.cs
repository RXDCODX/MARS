using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MARS.Shared.Hubs.Interfaces;

namespace MARS.Shared.Hubs;

public class TelegramusHub(ILogger<TelegramusHub> logger) : Hub<ITelegramusHub>
{
    public override Task OnConnectedAsync()
    {
        logger.LogInformation(
            "Client connected to TelegramusHub: {ConnectionId}",
            Context.ConnectionId
        );
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "Client disconnected from TelegramusHub: {ConnectionId}",
            Context.ConnectionId
        );
        return base.OnDisconnectedAsync(exception);
    }

    public Task LogError(string errorMessage)
    {
        logger.LogError("Client error: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    public Task TwitchMsg(string msg)
    {
        logger.LogInformation("TwitchMsg received: {Message}", msg);
        return Task.CompletedTask;
    }
}
