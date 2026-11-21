using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.RewardInput;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Сквозной шов «награда Twitch → MARS.Alerts → провод оверлейного хаба».
/// </summary>
/// <remarks>
/// Пинует точный байтовый payload, который браузер получит по SignalR: имя метода
/// на проводе, форма JSON и подставленный текст алерта. До этого тесты проверяли
/// слои отдельно (хендлеры на заглушках, реле на синтетике), и расхождение
/// между ними оставалось незамеченным.
/// </remarks>
public class RewardAlertWireTests
{
    private const string ExpectedText = "Pyro активировал Привет всем";

    private static HubEventRelay BuildRelayInternal(
        EventBroadcaster<TelegramusEvent> broadcaster,
        Mock<IClientProxy> proxy,
        TaskCompletionSource done
    )
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        var context = new Mock<IHubContext<AlertsHub>>();
        context.Setup(c => c.Clients).Returns(clients.Object);
        return new HubEventRelay(broadcaster, context.Object);
    }

    private static RewardRedeemedEvent RewardWithMedia() =>
        new()
        {
            RewardId = "reward-1",
            RewardCode = "pyro_alert",
            RewardTitle = "Pyro alert",
            Cost = 100,
            UserId = "123456789",
            UserName = "Pyro",
            UserInput = "Привет всем",
            User = new TwitchUser
            {
                TwitchId = "123456789",
                UserLogin = "pyro",
                DisplayName = "Pyro",
                ChatColor = "#FF0000",
            },
            Media = Media(),
        };

    private static MediaInfo Media() =>
        new()
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TextInfo = new MediaTextInfo
            {
                Text = "{user.name} активировал {user.text}",
                TextColor = "#FFFFFF",
                KeyWordsColor = "#00FF00",
                KeyWordSybmolDelimiter = '#',
            },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Image,
                FilePath = "https://example.com/alert.png",
                FileName = "alert.png",
                Extension = ".png",
                IsLocalFile = false,
            },
            PositionInfo = new MediaPositionInfo
            {
                Width = 400,
                Height = 300,
                IsProportion = true,
                IsHorizontalCenter = true,
                IsVerticallCenter = true,
                IsRotated = false,
                Rotation = 0,
                RandomCoordinates = false,
                IsUseOriginalWidthAndHeight = false,
                IsResizeRequires = false,
                XCoordinate = 0,
                YCoordinate = 0,
            },
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = "Pyro",
                Duration = 5,
                Priority = MediaAlertPriority.Normal,
                Volume = 100,
            },
            StylesInfo = new MediaStylesInfo { IsBorder = true },
        };

    [Fact]
    public async Task MediaRedemptionRendersAlertWithSubstitutedText()
    {
        var broadcaster = new EventBroadcaster<TelegramusEvent>();
        var listener = new TwitchMediaAlertsListener(
            NullLogger<TwitchMediaAlertsListener>.Instance,
            broadcaster
        );

        var proxy = new Mock<IClientProxy>();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        proxy
            .Setup(p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask)
            .Callback<string, object?[], CancellationToken>((_, _, _) => done.TrySetResult());

        var relay = BuildRelayInternal(broadcaster, proxy, done);
        await relay.StartAsync(TestContext.Current.CancellationToken);

        await listener.Handle(RewardWithMedia(), TestContext.Current.CancellationToken);

        await done.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await relay.StopAsync(TestContext.Current.CancellationToken);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
        );

        var method = (string)invocation.Arguments[0]!;
        var args = (object?[])invocation.Arguments[1]!;

        Assert.Equal("Alert", method);
        var alertEvent = Assert.IsType<AlertEvent>(args[0]);

        // Текст на проводе — ровно тот, что должен появиться в DOM после подстановки.
        var media = alertEvent.Media;
        Assert.NotNull(media);
        Assert.Equal(ExpectedText, media!.MediaInfo!.TextInfo!.Text);

        // Провод целиком совпадает с golden-файлом.
        var wireOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        wireOptions.Converters.Add(new JsonStringEnumConverter());
        var actual = JsonSerializer.Serialize(alertEvent, wireOptions);

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null
            && !File.Exists(
                Path.Combine(directory.FullName, "Hubs", "Fixtures", "reward-alert.wire.json")
            )
        )
        {
            directory = directory.Parent;
        }
        Assert.True(directory is not null, "Fixture directory not found.");
        var fixturePath = Path.Combine(
            directory!.FullName,
            "Hubs",
            "Fixtures",
            "reward-alert.wire.json"
        );

        var expected = await File.ReadAllTextAsync(
            fixturePath,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(expected.Trim(), actual);
    }

    [Fact]
    public async Task MediaRedemptionDoesNotDoubleFireCostMatchedAlert()
    {
        // RewardBoundAlertDispatcher с непустым UserInput/Media должен промолчать.
        var broadcaster = new EventBroadcaster<TelegramusEvent>();

        var alertSource = new Moq.Mock<MARS.Alerts.Application.Alerts.IEnabledAlertSource>();
        alertSource
            .Setup(s => s.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaInfo>());

        var dispatcher = new RewardBoundAlertDispatcher(
            alertSource.Object,
            new RickRollerService(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "-1" }
                    )
                    .Build(),
                broadcaster
            ),
            broadcaster,
            NullLogger<RewardBoundAlertDispatcher>.Instance
        );

        var user = new TwitchUser
        {
            TwitchId = "123456789",
            UserLogin = "pyro",
            DisplayName = "Pyro",
        };

        await dispatcher.DispatchRedemptionAsync(
            RewardWithMedia(),
            user,
            TestContext.Current.CancellationToken
        );

        // Никакого события в broadcaster'е не должно появиться.
        using var subscription = broadcaster.Subscribe();
        Assert.False(subscription.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RandomSoundRewardReachesRandomMemBranch()
    {
        var media = new MediaInfo
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
        var handler = new RandomSoundHandler(
            storage.Object,
            new RickRollerService(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?> { ["AppSettings:RickRoll:Chance"] = "-1" }
                    )
                    .Build(),
                broadcaster
            ),
            broadcaster,
            NullLogger<RandomSoundHandler>.Instance
        );

        var proxy = new Mock<IClientProxy>();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        proxy
            .Setup(p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask)
            .Callback<string, object?[], CancellationToken>((_, _, _) => done.TrySetResult());

        var relay = BuildRelayInternal(broadcaster, proxy, done);
        await relay.StartAsync(TestContext.Current.CancellationToken);

        await handler.HandleAsync(
            new RewardRedeemedEvent
            {
                RewardTitle = "🔊 Random sound",
                Cost = 10,
                UserId = "123456789",
                UserName = "Viewer",
                User = new TwitchUser
                {
                    TwitchId = "123456789",
                    UserLogin = "viewer",
                    DisplayName = "Viewer",
                },
            },
            TestContext.Current.CancellationToken
        );

        await done.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await relay.StopAsync(TestContext.Current.CancellationToken);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
        );

        Assert.Equal("RandomMem", (string)invocation.Arguments[0]!);
    }
}
