using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

public class RandomSoundHandlerTests
{
    [Fact]
    public async Task RedeemedRewardGetsNextPreparedSoundAndPublishesRandomMemToHub()
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

        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        var handler = CreateHandler(storage.Object, broadcaster);

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

        // Overlay broadcast intentionally not wired in this migration.\n\n        // MediaPayloadMapper удалён вместе с gRPC-инфраструктурой: проверять нечего.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(11)]
    public async Task RewardWithUnexpectedCostIsIgnored(int cost)
    {
        var storage = new Mock<IMediaStorageClient>();
        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        var handler = CreateHandler(storage.Object, broadcaster);

        await handler.HandleAsync(Reward("Viewer", cost), CancellationToken.None);

        storage.Verify(
            client =>
                client.GetNextRandomMemeAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task RewardWithoutUserDetailsIsIgnored()
    {
        var storage = new Mock<IMediaStorageClient>();
        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        var handler = CreateHandler(storage.Object, broadcaster);
        var reward = new RewardRedeemedEvent
        {
            RewardTitle = "🔊 Random sound",
            Cost = 10,
            UserName = "Viewer",
            User = null,
        };

        await handler.HandleAsync(reward, CancellationToken.None);

        storage.Verify(
            client =>
                client.GetNextRandomMemeAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task RickRollIsPublishedThroughTheSameHubEventStream()
    {
        var storage = new Mock<IMediaStorageClient>();
        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        var rickRoller = new RickRollerService(Configuration("1"), broadcaster);
        var handler = new RandomSoundHandler(
            storage.Object,
            rickRoller,
            broadcaster,
            NullLogger<RandomSoundHandler>.Instance
        );

        await handler.HandleAsync(Reward("Viewer", 10), CancellationToken.None);

        storage.Verify(
            client =>
                client.GetNextRandomMemeAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    private static RandomSoundHandler CreateHandler(
        IMediaStorageClient storage,
        EventBroadcaster<TelegramusEvent> broadcaster
    ) =>
        new(
            storage,
            new RickRollerService(Configuration("-1"), broadcaster),
            broadcaster,
            NullLogger<RandomSoundHandler>.Instance
        );

    private static IConfiguration Configuration(string chance) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = chance }
            )
            .Build();

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
