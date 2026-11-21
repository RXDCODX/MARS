using MARS.WaifuGacha.Models.Rest;

namespace MARS.WaifuGacha.Models.Rest;

/// <summary>Результат фумо-ролла для оверлея.</summary>
public sealed record FumoRollResultDto(
    FumoDto? Fumo,
    int CollectedCount,
    int TotalCount
);