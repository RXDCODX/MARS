using System.Collections.Generic;

namespace MARS.Shared.Models.Audio;

/// <summary>
/// Список сессий, которые сейчас заглушены микшером.
/// </summary>
public sealed class MutedSessionsResponse
{
    public IReadOnlyList<MutedSessionDto> Sessions { get; set; } = [];
}
