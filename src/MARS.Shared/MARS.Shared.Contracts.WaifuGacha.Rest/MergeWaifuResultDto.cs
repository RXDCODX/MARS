using MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат объединения супруга (награда «Свадьба») для оверлея.</summary>
public sealed record MergeWaifuResultDto(
    WaifuDto? Waifu,
    HusbandDto? Husband,
    string Avatar,
    string Color,
    bool IsNewMarriage
);
