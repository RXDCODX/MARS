namespace MARS.Shared.Contracts.Alerts;

/// <summary>
/// Отправка Fumo Friday для указанного пользователя.
/// </summary>
public sealed record FumoAlertCommandPayload(
    string DisplayName,
    string? Color
) : AlertCommandPayload;