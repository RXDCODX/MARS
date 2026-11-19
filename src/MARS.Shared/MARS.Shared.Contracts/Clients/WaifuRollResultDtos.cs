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

/// <summary>
/// Результат фумо-ролла для оверлея.
/// </summary>
public sealed record FumoRollResultDto(string FumoJson, int CollectedCount, int TotalCount);

/// <summary>
/// Результат мику-ролла для оверлея.
/// </summary>
public sealed record MikuRollResultDto(string MikuJson, int CollectedCount, int TotalCount);

/// <summary>
/// Результат лягушка-ролла для оверлея.
/// </summary>
public sealed record FrogRollResultDto(string FrogJson);

/// <summary>
/// Результат объединения супруга (награда «Свадьба») для оверлея.
/// </summary>
public sealed record MergeWaifuResultDto(
    string WaifuJson,
    string HusbandJson,
    string Avatar,
    string Color,
    bool IsNewMarriage
);

/// <summary>
/// Результат добавления новой вайфу для оверлея.
/// </summary>
public sealed record AddNewWaifuResultDto(
    string WaifuJson,
    string DisplayName,
    string Color,
    bool VipDropped,
    int RollsUntilGuarantee
);
