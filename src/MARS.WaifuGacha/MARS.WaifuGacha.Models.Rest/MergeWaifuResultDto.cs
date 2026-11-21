using MARS.WaifuGacha.Models.Rest;

namespace MARS.WaifuGacha.Models.Rest;

/// <summary>Результат объединения супруга (награда «Свадьба») для оверлея.</summary>
public sealed record MergeWaifuResultDto(
    WaifuDto? Waifu,
    HusbandDto? Husband,
    string Avatar,
    string Color,
    bool IsNewMarriage
);