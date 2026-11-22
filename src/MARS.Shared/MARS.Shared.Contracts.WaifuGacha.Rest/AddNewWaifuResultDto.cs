using MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат добавления новой вайфу для оверлея.</summary>
public sealed record AddNewWaifuResultDto(
    WaifuDto? Waifu,
    string DisplayName,
    string Color,
    bool VipDropped,
    int RollsUntilGuarantee
);
