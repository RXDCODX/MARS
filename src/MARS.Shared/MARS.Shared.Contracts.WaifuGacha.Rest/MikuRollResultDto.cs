using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат мику-ролла для оверлея.</summary>
public sealed record MikuRollResultDto(
    MikuModule? Module,
    int CollectedCount,
    int TotalCount,
    bool IsCooldown = false,
    int RemainingSeconds = 0
);
