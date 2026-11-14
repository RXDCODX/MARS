using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Grpc.Notifications;
using MARS.Shared.Contracts.Grpc.Models;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

public class RandomSoundHandlerTests
{
    [Fact]
    public async Task RedeemedRewardGetsNextPreparedSoundAndPublishesRandomMem()
    {
        var media = CreateMedia();
        var storage = new Mock<IMediaStorageClient>();
        storage
            .Setup(client =>
                client.GetNextRandomMemeAsync(
                    "Alerts/zvik",
                    "Viewer",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(media);

        var notifier = new Mock<ITelegramusNotifier>();
        notifier
            .Setup(instance => instance.RandomMem(It.IsAny<MediaDto>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(storage.Object, notifier.Object);

        await handler.HandleAsync(Reward("Viewer", 10), CancellationToken.None);

        storage.Verify(
            client =>
                client.GetNextRandomMemeAsync(
                    "Alerts/zvik",
                    "Viewer",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        notifier.Verify(
            instance => instance.RandomMem(It.Is<MediaDto>(dto => dto.MediaInfo == media)),
            Times.Once
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(11)]
    public async Task RewardWithUnexpectedCostIsIgnored(int cost)
    {
        var storage = new Mock<IMediaStorageClient>();
        var notifier = new Mock<ITelegramusNotifier>();
        var handler = CreateHandler(storage.Object, notifier.Object);

        await handler.HandleAsync(Reward("Viewer", cost), CancellationToken.None);

        storage.Verify(
            client => client.GetNextRandomMemeAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
        notifier.Verify(
            instance => instance.RandomMem(It.IsAny<MediaDto>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RewardWithoutUserDetailsIsIgnored()
    {
        var storage = new Mock<IMediaStorageClient>();
        var notifier = new Mock<ITelegramusNotifier>();
        var handler = CreateHandler(storage.Object, notifier.Object);
        var reward = new RewardRedeemedEvent
        {
            RewardTitle = "🔊 Random sound",
            Cost = 10,
            UserName = "Viewer",
            User = null,
        };

        await handler.HandleAsync(reward, CancellationToken.None);

        storage.Verify(
            client => client.GetNextRandomMemeAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    private static RandomSoundHandler CreateHandler(
        IMediaStorageClient storage,
        ITelegramusNotifier notifier
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "-1" }
            )
            .Build();

        return new RandomSoundHandler(
            storage,
            notifier,
            new RickRollerService(notifier, configuration),
            NullLogger<RandomSoundHandler>.Instance
        );
    }

    private static RewardRedeemedEvent Reward(string userName, int cost) =>
        new()
        {
            RewardTitle = "🔊 Random sound",
            Cost = cost,
            UserId = "123456789",
            UserName = userName,
            User = new TwitchUser
            {
                TwitchId = "123456789",
                UserLogin = userName.ToLowerInvariant(),
                DisplayName = userName,
            },
        };

    private static MediaInfo CreateMedia() =>
        new()
        {
            TextInfo = new MediaTextInfo(),
            FileInfo = new MediaFileInfo
            {
                Extension = ".mp3",
                FileName = "sample.mp3",
                FilePath = "/Alerts/zvik/sample.mp3",
                Type = MediaType.Audio,
                IsLocalFile = true,
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "Viewer" },
            StylesInfo = new MediaStylesInfo(),
        };
}
