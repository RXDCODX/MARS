namespace MARS.Shared.Contracts.Twitch.Rest;

public sealed class RewardRefundRequest
{
    public string RewardId { get; set; } = string.Empty;

    public List<string> RedemptionIds { get; set; } = [];

    public string? Reason { get; set; }
}
