namespace MARS.WaifuGacha.Models.Rest;

/// <summary>
/// Базовая сущность супруга (хоста) для межсервисного обмена.
/// </summary>
public sealed record HusbandDto(
    string TwitchId,
    string? WaifuBrideId,
    bool IsPrivated,
    long OrderCount,
    DateTime WhenOrdered
);
