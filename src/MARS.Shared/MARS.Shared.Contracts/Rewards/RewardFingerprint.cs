namespace MARS.Shared.Contracts.Rewards;

public readonly record struct RewardFingerprint(int Cost, string NormalizedEmoji)
{
    public override string ToString() => $"{Cost}:{NormalizedEmoji}";
}
