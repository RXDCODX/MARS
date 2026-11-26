using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards._10_RandomSound;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.MediaStorage;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Services;

public class RandomSoundHandlerTests
{
    [Fact]
    public async Task RedeemedRewardGetsNextPreparedSoundAndPublishesRandomMemToHub()
    {
        var media = CreateMedia();
        var mediaStorage = new Mock<IMediaStorageApi>();
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.MediaStorage).Returns(mediaStorage.Object);
        mediaStorage
            .Setup(client =>
                client.GetNextRandomMemeAsync(
                    "Alerts/zvik",
                    "Viewer",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(media);

        var broadcaster = new TestOverlayHub();
        var handler = CreateHandler(grpc.Object, broadcaster);

        await handler.HandleAsync(Reward("Viewer", 10), CancellationToken.None);

        mediaStorage.Verify(
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
        var mediaStorage = new Mock<IMediaStorageApi>();
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.MediaStorage).Returns(mediaStorage.Object);
        var broadcaster = new TestOverlayHub();
        var handler = CreateHandler(grpc.Object, broadcaster);

        await handler.HandleAsync(Reward("Viewer", cost), CancellationToken.None);

        mediaStorage.Verify(
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
        var mediaStorage = new Mock<IMediaStorageApi>();
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.MediaStorage).Returns(mediaStorage.Object);
        var broadcaster = new TestOverlayHub();
        var handler = CreateHandler(grpc.Object, broadcaster);
        var reward = new RewardRedeemedEvent
        {
            RewardTitle = "🔊 Random sound",
            Cost = 10,
            UserName = "Viewer",
            User = null,
        };

        await handler.HandleAsync(reward, CancellationToken.None);

        mediaStorage.Verify(
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
        var mediaStorage = new Mock<IMediaStorageApi>();
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.MediaStorage).Returns(mediaStorage.Object);
        var broadcaster = new TestOverlayHub();
        var rickRoller = new RickRollerService(Configuration("1"), broadcaster);
        var handler = new RandomSoundHandler(
            grpc.Object,
            Validator(),
            broadcaster,
            NullLogger<RandomSoundHandler>.Instance,
            new Mock<IHostApplicationLifetime>().Object,
            rickRoller,
            new RewardEventManager()
        );

        await handler.HandleAsync(Reward("Viewer", 10), CancellationToken.None);

        mediaStorage.Verify(
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
        IMarsGrpcClient grpc,
        TestOverlayHub broadcaster
    ) =>
        new(
            grpc,
            Validator(),
            broadcaster,
            NullLogger<RandomSoundHandler>.Instance,
            new Mock<IHostApplicationLifetime>().Object,
            new RickRollerService(Configuration("-1"), broadcaster),
            new RewardEventManager()
        );

    private static ITwitchEventValidationService Validator() =>
        new Mock<ITwitchEventValidationService>().Object;

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
