using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Резолв файла награды: событие, привязка по <c>TwitchGuid</c>, цена в баллах.
/// </summary>
/// <remarks>
/// Порядок — часть поведения, а не деталь реализации. Файл из события точнее
/// привязки, привязка точнее цены, а цена не уникальна: награда с тем же
/// номиналом без привязки не должна получить чужой файл.
/// </remarks>
public class RewardMediaResolverTests
{
    private readonly Mock<IMediaStorageClient> _storage = new();

    [Fact]
    public async Task EventMediaWinsAndStorageIsNotAsked()
    {
        var media = Media();
        var reward = Reward();
        reward.Media = media;
        var resolver = Create();

        var resolved = await resolver.ResolveAsync(reward, TestContext.Current.CancellationToken);

        Assert.Same(media, Assert.Single(resolved));
        _storage.Verify(
            client => client.GetAllAlertsAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MatchesByTwitchGuid()
    {
        var guid = Guid.NewGuid();
        var match = Media(guid: guid);
        var other = Media(guid: Guid.NewGuid(), cost: 100);
        Storage(match, other);
        var reward = Reward(rewardId: guid.ToString(), cost: 500);

        var resolved = await Create().ResolveAsync(reward, TestContext.Current.CancellationToken);

        Assert.Same(match, Assert.Single(resolved));
    }

    [Fact]
    public async Task GuidWinsOverCost()
    {
        var guid = Guid.NewGuid();
        var byGuid = Media(guid: guid, cost: 999);
        var byCost = Media(cost: 100);
        Storage(byCost, byGuid);
        var reward = Reward(rewardId: guid.ToString(), cost: 100);

        var resolved = await Create().ResolveAsync(reward, TestContext.Current.CancellationToken);

        Assert.Same(byGuid, Assert.Single(resolved));
    }

    [Fact]
    public async Task FallsBackToCostWhenGuidDoesNotMatch()
    {
        var byCost = Media(cost: 100);
        Storage(Media(guid: Guid.NewGuid(), cost: 0), byCost);
        var reward = Reward(rewardId: Guid.NewGuid().ToString(), cost: 100);

        var resolved = await Create().ResolveAsync(reward, TestContext.Current.CancellationToken);

        Assert.Same(byCost, Assert.Single(resolved));
    }

    [Fact]
    public async Task ZeroCostDoesNotMatchAnything()
    {
        Storage(Media(cost: 0));
        var reward = Reward(rewardId: Guid.NewGuid().ToString(), cost: 0);

        var resolved = await Create().ResolveAsync(reward, TestContext.Current.CancellationToken);

        Assert.Empty(resolved);
    }

    [Fact]
    public async Task InvalidRewardIdStillFallsBackToCost()
    {
        var byCost = Media(cost: 100);
        Storage(byCost);

        var resolved = await Create()
            .ResolveAsync(
                Reward(rewardId: "not-a-guid", cost: 100),
                TestContext.Current.CancellationToken
            );

        Assert.Same(byCost, Assert.Single(resolved));
    }

    /// <summary>
    /// Недоступное хранилище — это не «файла нет»: награда всё равно
    /// активируется, иначе одна зависимость гасила бы все награды сразу.
    /// </summary>
    [Fact]
    public async Task UnavailableStorageReturnsEmptyAndDoesNotThrow()
    {
        _storage
            .Setup(client => client.GetAllAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaInfo>?)null);

        var resolved = await Create()
            .ResolveAsync(
                Reward(rewardId: Guid.NewGuid().ToString(), cost: 100),
                TestContext.Current.CancellationToken
            );

        Assert.Empty(resolved);
    }

    [Fact]
    public async Task EmptyStorageReturnsEmpty()
    {
        _storage
            .Setup(client => client.GetAllAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resolved = await Create()
            .ResolveAsync(
                Reward(rewardId: Guid.NewGuid().ToString(), cost: 100),
                TestContext.Current.CancellationToken
            );

        Assert.Empty(resolved);
    }

    private RewardMediaResolver Create() =>
        new(_storage.Object, NullLogger<RewardMediaResolver>.Instance);

    private void Storage(params MediaInfo[] alerts) =>
        _storage
            .Setup(client => client.GetAllAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

    private static RewardRedeemedEvent Reward(string? rewardId = null, int cost = 0) =>
        new()
        {
            RewardId = rewardId ?? string.Empty,
            Cost = cost,
            UserId = "42",
            UserName = "Pyro",
        };

    private static MediaInfo Media(int cost = 0, Guid? guid = null) =>
        new()
        {
            TextInfo = new MediaTextInfo(),
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alerts/alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = "alert",
                TwitchPointsCost = cost,
                TwitchGuid = guid ?? Guid.Empty,
            },
            StylesInfo = new MediaStylesInfo(),
        };
}
