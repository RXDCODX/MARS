using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.TwitchRewards.RewardInput;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Проверяет фильтрацию и ранний выход диспетчера алертов по наградам.
/// </summary>
public class RewardBoundAlertDispatcherTests
{
    private static RewardBoundAlertDispatcher CreateDispatcher(IEnabledAlertSource source)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "-1" }
            )
            .Build();

        var hubContext = new Mock<IHubContext<AlertsHub, IAlertsHub>>();
        var clients = new Mock<IHubClients<IAlertsHub>>();
        var alertsHub = new Mock<IAlertsHub>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);
        clients.Setup(x => x.All).Returns(alertsHub.Object);
        return new RewardBoundAlertDispatcher(
            source,
            new RickRollerService(configuration, hubContext.Object),
            hubContext.Object,
            NullLogger<RewardBoundAlertDispatcher>.Instance
        );
    }

    private static Mock<IEnabledAlertSource> CreateSource()
    {
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaInfo>?)Array.Empty<MediaInfo>());
        return source;
    }

    private static TwitchUser Viewer() =>
        new()
        {
            TwitchId = "42",
            UserLogin = "viewer",
            DisplayName = "Viewer",
        };

    [Fact]
    public async Task DispatchRewardInputAsync_LoadsAlertsForGuidReward()
    {
        var source = CreateSource();
        var dispatcher = CreateDispatcher(source.Object);
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRewardInputAsync(
            new ChatEventMessageEvent { CustomRewardId = Guid.NewGuid().ToString() },
            Viewer(),
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }

    [Fact]
    public async Task DispatchRewardInputAsync_DoesNotLoadAlertsForNonGuidReward()
    {
        var source = CreateSource();
        var dispatcher = CreateDispatcher(source.Object);

        await dispatcher.DispatchRewardInputAsync(
            new ChatEventMessageEvent { CustomRewardId = "not-a-guid" },
            Viewer(),
            TestContext.Current.CancellationToken
        );

        source.Verify(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchRedemptionAsync_LoadsAlertsWithoutUserInputOrMedia()
    {
        var source = CreateSource();
        var dispatcher = CreateDispatcher(source.Object);
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 500 },
            Viewer(),
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }

    [Fact]
    public async Task DispatchRedemptionAsync_SkipsAlertsWhenUserInputExists()
    {
        var source = CreateSource();
        var dispatcher = CreateDispatcher(source.Object);

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 500, UserInput = "already handled" },
            Viewer(),
            TestContext.Current.CancellationToken
        );

        source.Verify(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchRedemptionAsync_BroadcastsCostMatchedAlertWithMedia()
    {
        var media = new MediaInfo
        {
            Id = Guid.NewGuid(),
            TextInfo = new MediaTextInfo { Text = "за 182 балла", TriggerWord = string.Empty },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Image,
                FilePath = "/Alerts/uploaded_mems/cost.png",
                FileName = "cost.png",
                Extension = ".png",
                IsLocalFile = true,
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = "CostAlert",
                IsEnabled = true,
                TwitchPointsCost = 182,
                Priority = MediaAlertPriority.Normal,
                Duration = 5,
                Volume = 100,
            },
            StylesInfo = new MediaStylesInfo(),
        };

        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaInfo> { media });

        var hubContext = new Mock<IHubContext<AlertsHub, IAlertsHub>>();
        var clients = new Mock<IHubClients<IAlertsHub>>();
        var alertsHub = new Mock<IAlertsHub>();
        hubContext.Setup(x => x.Clients).Returns(clients.Object);
        clients.Setup(x => x.All).Returns(alertsHub.Object);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "-1" }
            )
            .Build();
        var dispatcher = new RewardBoundAlertDispatcher(
            source.Object,
            new RickRollerService(configuration, hubContext.Object),
            hubContext.Object,
            NullLogger<RewardBoundAlertDispatcher>.Instance
        );

        var ct = TestContext.Current.CancellationToken;
        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent { Cost = 182 },
            Viewer(),
            ct
        );

    }

    [Fact]
    public async Task DispatchRedemptionAsync_SkipsAlertsWhenRewardContainsMedia()
    {
        var source = CreateSource();
        var dispatcher = CreateDispatcher(source.Object);

        await dispatcher.DispatchRedemptionAsync(
            new RewardRedeemedEvent
            {
                Cost = 500,
                Media = new MediaInfo
                {
                    TextInfo = new(),
                    FileInfo = new()
                    {
                        Type = MediaType.Audio,
                        FilePath = "x",
                        FileName = "x",
                        Extension = ".mp3",
                    },
                    PositionInfo = new(),
                    MetaInfo = new() { DisplayName = "x" },
                    StylesInfo = new(),
                },
            },
            Viewer(),
            TestContext.Current.CancellationToken
        );

        source.Verify(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}