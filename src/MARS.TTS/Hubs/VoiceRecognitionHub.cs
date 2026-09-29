using MARS.TTS.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MARS.TTS.Hubs;

/// <summary>
/// SignalR hub for TTS delivery to AudioController.
/// AudioController connects as a consumer client and receives TTS playback requests.
/// </summary>
public class VoiceRecognitionHub(ILogger<VoiceRecognitionHub> logger)
    : Hub<IVoiceRecognitionHub>
{
    private const string TtsConsumersGroupName = "tts-consumers";

    public async Task RegisterAsTtsConsumer()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, TtsConsumersGroupName);
        logger.LogInformation("TTS consumer registered: {ConnectionId}", Context.ConnectionId);
    }

    public async Task UnregisterAsTtsConsumer()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TtsConsumersGroupName);
        logger.LogInformation("TTS consumer unregistered: {ConnectionId}", Context.ConnectionId);
    }

    public Task ReportTtsPlaybackStarted(string text)
    {
        logger.LogInformation(
            "TTS playback started by consumer {ConnectionId}: {Text}",
            Context.ConnectionId,
            text
        );
        return Task.CompletedTask;
    }

    public Task ReportTtsPlaybackCompleted(string text, TimeSpan duration)
    {
        logger.LogInformation(
            "TTS playback completed by consumer {ConnectionId}: {Text}, duration={Duration}",
            Context.ConnectionId,
            text,
            duration
        );
        return Task.CompletedTask;
    }

    public Task ReportTtsPlaybackFailed(string text, string error)
    {
        logger.LogWarning(
            "TTS playback failed on consumer {ConnectionId}: {Text}, error={Error}",
            Context.ConnectionId,
            text,
            error
        );
        return Task.CompletedTask;
    }

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected to TtsHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TtsConsumersGroupName);
        logger.LogInformation(
            "Client disconnected from TtsHub: {ConnectionId}",
            Context.ConnectionId
        );
        await base.OnDisconnectedAsync(exception);
    }
}
