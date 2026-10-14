namespace MARS.Shared.Models.Audio;

/// <summary>
/// Одна заглушённая аудиосессия приложения.
/// </summary>
public sealed class MutedSessionDto
{
    public uint ProcessId { get; set; }

    public string ProcessName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
