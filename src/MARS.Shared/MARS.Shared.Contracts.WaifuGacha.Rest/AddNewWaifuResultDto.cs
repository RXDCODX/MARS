using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>Результат добавления новой вайфу для оверлея.</summary>
public sealed record AddNewWaifuResultDto(
    Waifu? Waifu,
    string DisplayName,
    string Color,
    bool VipDropped,
    int RollsUntilGuarantee
);
