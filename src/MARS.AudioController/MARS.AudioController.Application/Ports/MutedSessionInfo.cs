namespace MARS.AudioController.Application.Ports;

/// <summary>
/// Информация об одной заглушённой аудиосессии.
/// </summary>
public sealed record MutedSessionInfo(uint ProcessId, string ProcessName, string DisplayName);
