namespace MARS.Shared.Contracts.Alerts;

/// <summary>
/// Снапшот определения награды из MARS.Alerts для синхронизации в MARS.Twitch.
/// </summary>
public sealed record RewardDefinitionSnapshot(
    int Cost,
    string Title,
    string Prompt,
    string BackgroundColor,
    bool IsUserInputRequired,
    bool IsActive
);
