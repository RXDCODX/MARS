using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат жаба-ролла для оверлея.</summary>
public sealed record FrogRollResultDto(
    Frog? Frog,
    bool IsCooldown = false,
    int RemainingSeconds = 0
);
