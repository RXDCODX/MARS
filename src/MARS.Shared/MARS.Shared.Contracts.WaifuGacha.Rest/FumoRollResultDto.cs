using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат фумо-ролла для оверлея.</summary>
public sealed record FumoRollResultDto(Fumo? Fumo, int CollectedCount, int TotalCount);
