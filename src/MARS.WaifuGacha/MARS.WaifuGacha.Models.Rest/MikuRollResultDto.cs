using MARS.WaifuGacha.Models.Rest;

namespace MARS.WaifuGacha.Models.Rest;

/// <summary>Результат мику-ролла для оверлея.</summary>
public sealed record MikuRollResultDto(MikuModuleDto? Module, int CollectedCount, int TotalCount);
