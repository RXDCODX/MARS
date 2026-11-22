using MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат мику-ролла для оверлея.</summary>
public sealed record MikuRollResultDto(MikuModuleDto? Module, int CollectedCount, int TotalCount);
