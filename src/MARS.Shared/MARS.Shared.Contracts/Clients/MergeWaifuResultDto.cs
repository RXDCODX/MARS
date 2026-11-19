namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Результат объединения супруга (награда «Свадьба») для оверлея.
/// </summary>
public sealed record MergeWaifuResultDto(
    string WaifuJson,
    string HusbandJson,
    string Avatar,
    string Color,
    bool IsNewMarriage
);
