using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Media;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using Wolverine;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Издатель событий погашения наград в брокер.
///
/// Это единственное место, откуда MARS.Alerts узнаёт о награде: если событие не
/// уйдёт, три десятка обработчиков молча ничего не покажут. Проверяется состав
/// события и отказ от событий без Reward.Id.
/// </summary>
public class RewardRedemptionPublisherTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly Mock<IMessageBus> _bus = new();
    private readonly List<object> _published = [];
    private readonly RewardRedemptionPublisher _publisher;

    public RewardRedemptionPublisherTests()
    {
        _bus.Setup(instance =>
                instance.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>())
            )
            .Callback<object, DeliveryOptions>((m, _) => _published.Add(m))
            .Returns(ValueTask.CompletedTask);

        _publisher = new RewardRedemptionPublisher(
            OfflineEventSub.Create(),
            PassingValidationService.Instance,
            _bus.Object,
            new FakeUserEnsureService(_factory),
            Mock.Of<IMediaStorageClient>(),
            _factory,
            new TestLifetime(),
            NullLogger<RewardRedemptionPublisher>.Instance
        );
    }

    [Fact]
    public async Task RedemptionIsPublishedAsRewardRedeemedEvent()
    {
        await PublishAsync(Redemption(rewardId: "reward-1", title: "Аяка"));

        var published = Assert.Single(_published);
        var rewardEvent = Assert.IsType<RewardRedeemedEvent>(published);
        Assert.Equal("Аяка", rewardEvent.RewardTitle);
    }

    /// <summary>
    /// Событие без Reward.Id игнорируется: по нему нельзя сопоставить награду с
    /// медиа, и подписчики получили бы событие без содержимого.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RedemptionWithoutRewardIdIsIgnored(string? rewardId)
    {
        await PublishAsync(Redemption(rewardId, title: "Аяка"));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task EventCarriesUserAndReward()
    {
        await PublishAsync(Redemption(rewardId: "reward-1", title: "Аяка"));

        var message = Assert.IsType<RewardRedeemedEvent>(Assert.Single(_published));

        Assert.Equal("123456789", message.UserId);
        Assert.Equal("Аяка", message.RewardTitle);
    }

    [Fact]
    public async Task MediaIsAttachedWhenRewardHasIt()
    {
        await using var context = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var mediaInfoId = Guid.NewGuid();
        context.TwitchUsers.Add(
            new TwitchUser
            {
                TwitchId = "123456789",
                UserLogin = "pyro",
                DisplayName = "Pyro",
            }
        );
        context.ChannelRewards.Add(
            new ChannelRewardRecord
            {
                TwitchRewardId = "11111111-1111-1111-1111-111111111111",
                Title = "Аяка",
                Cost = 100,
                MediaInfoId = mediaInfoId,
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var storage = new Mock<IMediaStorageClient>();
        storage
            .Setup(instance =>
                instance.GetMediaInfoAsync(mediaInfoId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new MediaInfo
                {
                    TextInfo = new MediaTextInfo { Text = "текст" },
                    FileInfo = new MediaFileInfo
                    {
                        Type = MediaType.Audio,
                        FilePath = "memory/аяка.mp3",
                        FileName = "аяка.mp3",
                        Extension = "mp3",
                    },
                    PositionInfo = new MediaPositionInfo(),
                    MetaInfo = new MediaMetaInfo { DisplayName = "Аяка" },
                    StylesInfo = new MediaStylesInfo(),
                }
            );
        var publisher = CreateWith(storage.Object);

        await PublishAsync(publisher, Redemption("reward-1", "Аяка"));

        var message = Assert.IsType<RewardRedeemedEvent>(Assert.Single(_published));
        Assert.NotNull(message.Media);
        Assert.Equal("аяка.mp3", message.Media!.FileInfo.FileName);
    }

    [Fact]
    public async Task PublishFailureDoesNotThrow()
    {
        _bus.Setup(instance =>
                instance.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>())
            )
            .ThrowsAsync(new InvalidOperationException("брокер недоступен"));

        await PublishAsync(Redemption("reward-1", "Аяка"));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task RejectedRedemptionIsNotPublished()
    {
        var publisher = new RewardRedemptionPublisher(
            OfflineEventSub.Create(),
            PassingValidationService.Rejecting("Это работает только на основном канале"),
            _bus.Object,
            new FakeUserEnsureService(_factory),
            Mock.Of<IMediaStorageClient>(),
            _factory,
            new TestLifetime(),
            NullLogger<RewardRedemptionPublisher>.Instance
        );

        await PublishAsync(publisher, Redemption("reward-1", "Аяка"));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task StartAndStopHookUpEventSub()
    {
        await _publisher.StartAsync(TestContext.Current.CancellationToken);
        await _publisher.StopAsync(TestContext.Current.CancellationToken);
    }

    private RewardRedemptionPublisher CreateWith(IMediaStorageClient storage) =>
        new(
            OfflineEventSub.Create(),
            PassingValidationService.Instance,
            _bus.Object,
            new FakeUserEnsureService(_factory),
            storage,
            _factory,
            new TestLifetime(),
            NullLogger<RewardRedemptionPublisher>.Instance
        );

    private Task PublishAsync(ChannelPointsCustomRewardRedemptionArgs args) =>
        PublishAsync(_publisher, args);

    private static async Task PublishAsync(
        RewardRedemptionPublisher publisher,
        ChannelPointsCustomRewardRedemptionArgs args
    )
    {
        var method = typeof(RewardRedemptionPublisher).GetMethod(
            "OnRedemptionAdd",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(publisher, [null, args])!;
    }

    private static ChannelPointsCustomRewardRedemptionArgs Redemption(
        string? rewardId,
        string title
    ) =>
        new()
        {
            Payload = new EventSubNotificationPayload<ChannelPointsCustomRewardRedemption>
            {
                Event = new ChannelPointsCustomRewardRedemption
                {
                    BroadcasterUserId = MARS.TwitchCore.Extensions.TwitchConstants.ChannelId,
                    BroadcasterUserLogin = MARS.TwitchCore.Extensions.TwitchConstants.Channel,
                    UserId = "123456789",
                    UserLogin = "pyro",
                    UserName = "pyro",
                    Reward = new TwitchLib.EventSub.Core.Models.ChannelPoints.RedemptionReward
                    {
                        Id = rewardId!,
                        Title = title,
                        Cost = 100,
                    },
                },
            },
        };
}
