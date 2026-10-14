namespace MARS.Shared.Models.Audio;

/// <summary>
/// Ответ на unmute микшера: сколько сессий было разблокировано.
/// </summary>
public sealed class UnmuteMixerResponse
{
    public int UnmutedSessionCount { get; set; }
}
