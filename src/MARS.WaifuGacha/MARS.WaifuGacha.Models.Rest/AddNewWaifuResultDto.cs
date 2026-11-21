using MARS.WaifuGacha.Models.Rest;

namespace MARS.WaifuGacha.Models.Rest;

/// <summary>Результат добавления новой вайфу для оверлея.</summary>
public sealed record AddNewWaifuResultDto(
    WaifuDto? Waifu,
    string DisplayName,
    string Color,
    bool VipDropped,
    int RollsUntilGuarantee
);