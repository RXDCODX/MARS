namespace MARS.Shared.Contracts.Alerts;

/// <summary>
/// Активация ADHD-эффекта. Пустое <see cref="Seconds"/> (или 0) — переключение
/// в перманентный режим.
/// </summary>
public sealed record AdhdAlertCommandPayload(int Seconds) : AlertCommandPayload;
