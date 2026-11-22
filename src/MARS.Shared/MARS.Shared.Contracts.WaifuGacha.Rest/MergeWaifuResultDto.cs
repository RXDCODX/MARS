using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат объединения супруга (награда «Свадьба») для оверлея.</summary>
public sealed record MergeWaifuResultDto(Waifu? Waifu, Husband? Husband, bool IsNewMarriage);
