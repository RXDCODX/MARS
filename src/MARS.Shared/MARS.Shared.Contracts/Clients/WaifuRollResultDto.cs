namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Результат вайфу-ролла (награда «Поиск супруга») для оверлея.
/// </summary>
public sealed record WaifuRollResultDto(
    string WaifuJson,
    string HostDisplayName,
    string HostColor,
    bool IsCooldown,
    int RemainingSeconds
);
