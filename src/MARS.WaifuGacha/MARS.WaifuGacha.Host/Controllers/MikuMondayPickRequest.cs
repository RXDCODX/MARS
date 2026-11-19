namespace MARS.WaifuGacha.Controllers;

/// <summary>
/// Тело запроса выбора трека Miku Monday.
/// </summary>
public sealed class MikuMondayPickRequest
{
    public string TwitchUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsStreamer { get; set; }
}
