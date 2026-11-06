using System.Collections.Generic;

namespace MARS.Shared.Contracts.Models.Audio;

/// <summary>
/// Ответ на mute микшера: сколько сессий было заглушено и какие именно.
/// </summary>
public sealed class MuteMixerResponse
{
    public int MutedSessionCount { get; set; }

    public IReadOnlyList<string> MutedProcessNames { get; set; } = [];
}
