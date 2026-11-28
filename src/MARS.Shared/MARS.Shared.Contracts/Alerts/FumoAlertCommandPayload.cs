using System.Diagnostics.CodeAnalysis;
namespace MARS.Shared.Contracts.Alerts;

/// <summary>
/// Отправка Fumo Friday для указанного пользователя.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record FumoAlertCommandPayload(
    string DisplayName,
    string? Color
) : AlertCommandPayload;