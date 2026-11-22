using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>
/// Результат вайфу-ролла (награда «Поиск супруга») для оверлея.
/// Сущности передаются структурно, без JSON-строк.
/// </summary>
public sealed record WaifuRollResultDto(
    Waifu? Waifu,
    Husband? Husband,
    bool IsCooldown,
    int RemainingSeconds
);
