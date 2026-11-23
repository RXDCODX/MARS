namespace MARS.Shared.Contracts.Twitch.Rest;

public sealed class MiniGameResultRequest
{
    public string Game { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public bool WithWaifu { get; set; }
}
