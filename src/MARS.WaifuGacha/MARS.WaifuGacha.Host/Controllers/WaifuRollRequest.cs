namespace MARS.WaifuGacha.Controllers;

/// <summary>
/// Тело запроса вайфу-ролла по нику хоста.
/// </summary>
public sealed class WaifuRollRequest
{
    public string HostName { get; set; } = string.Empty;
}
