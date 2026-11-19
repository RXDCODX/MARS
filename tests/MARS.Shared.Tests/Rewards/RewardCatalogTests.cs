using MARS.Shared.Contracts.Rewards;

namespace MARS.Shared.Tests.Rewards;

public class RewardCatalogTests
{
    [Fact]
    public void Catalog_Contains49UniqueRewards()
    {
        var all = RewardCatalog.All;

        Assert.Equal(49, all.Count);
        Assert.Equal(all.Count, all.Select(item => item.Code).Distinct().Count());
        Assert.Equal(all.Count, all.Select(item => item.Fingerprint).Distinct().Count());
    }

    [Fact]
    public void Groups_HaveUniqueCostAndAtLeastTwoMembers()
    {
        var groups = RewardCatalog
            .All.Where(item => item.Group is not null)
            .GroupBy(item => item.Group!);

        foreach (var group in groups)
        {
            Assert.True(group.Count() >= 2);
            Assert.Single(group.Select(item => item.ExpectedCost).Distinct());
        }
    }

    [Fact]
    public void EmojiExtractor_NormalizesVariationSelector()
    {
        Assert.Equal("📽", RewardEmojiExtractor.Normalize("📽️"));
        Assert.Equal("🎲", RewardEmojiExtractor.FirstGrapheme("🎲 Случайная награда!"));
    }
}
