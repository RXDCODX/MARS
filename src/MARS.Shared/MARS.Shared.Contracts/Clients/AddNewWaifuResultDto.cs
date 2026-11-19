namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Результат добавления новой вайфу для оверлея.
/// </summary>
public sealed record AddNewWaifuResultDto(
    string WaifuJson,
    string DisplayName,
    string Color,
    bool VipDropped,
    int RollsUntilGuarantee
);
