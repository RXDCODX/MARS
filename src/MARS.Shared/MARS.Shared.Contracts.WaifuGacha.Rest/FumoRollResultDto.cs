using MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат фумо-ролла для оверлея.</summary>
public sealed record FumoRollResultDto(FumoDto? Fumo, int CollectedCount, int TotalCount);
