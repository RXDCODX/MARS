namespace MARS.Twitch.Application.Models;

public sealed class TwitchConsoleSendRequest
{
    public string Channel { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
