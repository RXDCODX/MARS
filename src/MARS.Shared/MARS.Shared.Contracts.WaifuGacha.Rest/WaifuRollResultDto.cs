using MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>
/// Результат вайфу-ролла (награда «Поиск супруга») для оверлея.
/// Сущности передаются структурно, без JSON-строк.
/// </summary>
public sealed record WaifuRollResultDto(
    WaifuDto? Waifu,
    HusbandDto? Husband,
    string HostDisplayName,
    string HostColor,
    bool IsCooldown,
    int RemainingSeconds
);
