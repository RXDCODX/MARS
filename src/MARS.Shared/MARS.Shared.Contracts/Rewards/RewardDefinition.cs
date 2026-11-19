namespace MARS.Shared.Contracts.Rewards;

public sealed record RewardDefinition
{
    public required string Code { get; init; }
    public required int ExpectedCost { get; init; }
    public required string NormalizedEmoji { get; init; }
    public required string LegacyTitle { get; init; }
    public string? Group { get; init; }
    public bool RequiresUserInput { get; init; }
    public bool RequiresFollower { get; init; }
    public bool ExcludedFromRandomPool { get; init; }
    public TimeSpan? UserCooldown { get; init; }

    public RewardFingerprint Fingerprint => new(ExpectedCost, NormalizedEmoji);
}
