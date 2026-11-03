using System.Text.Json;
using MARS.Shared.Clients;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Models.Media;
using MARS.TwitchCore.Services.HelloVideos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Нотификатор приветственного видео. Проверяется, что событие передаётся
/// через общий JSON-метод SignalR, а не отдельный RabbitMQ reward-маршрут.
/// Без медиа уведомление не публикуется.
/// </summary>
public class HelloVideoNotifierTests
{
    [Fact]
    public async Task AlertIsPublishedThroughUniversalJsonChannelWithMedia()
    {
        var media = Media();
        var notifier = new Mock<ITelegramusNotifier>();
        var storage = Storage(media);
        string? sentJson = null;

        notifier
            .Setup(client => client.TwitchEventSub(It.IsAny<string>()))
            .Callback<string>(json => sentJson = json)
            .Returns(Task.CompletedTask);

        await CreateNotifier(notifier.Object, storage.Object).SendAlertAsync(Alert(media.Id));

        notifier.Verify(client => client.TwitchEventSub(It.IsAny<string>()), Times.Once);

        using var document = JsonDocument.Parse(Assert.IsType<string>(sentJson));
        var reward = document.RootElement.GetProperty("mars").GetProperty("reward");
        Assert.Equal("mars.reward.activated", reward.GetProperty("type").GetString());
        Assert.Equal("hello-video", reward.GetProperty("eventId").GetString()!.Split(':')[0]);

        var payload = reward.GetProperty("payload");
        Assert.Equal("hellovideo", payload.GetProperty("alertKey").GetString());
        Assert.Equal("Приветственное видео", payload.GetProperty("rewardTitle").GetString());
        Assert.Equal(
            "hello.mp4",
            payload
                .GetProperty("media")[0]
                .GetProperty("fileInfo")
                .GetProperty("fileName")
                .GetString()
        );
    }

    [Fact]
    public async Task AuthorIsAttachedToUniversalEvent()
    {
        var media = Media();
        var notifier = new Mock<ITelegramusNotifier>();
        var storage = Storage(media);
        string? sentJson = null;

        notifier
            .Setup(client => client.TwitchEventSub(It.IsAny<string>()))
            .Callback<string>(json => sentJson = json)
            .Returns(Task.CompletedTask);

        await CreateNotifier(notifier.Object, storage.Object)
            .SendAlertAsync(Alert(media.Id, "Pyro", "#FF0000"));

        using var document = JsonDocument.Parse(Assert.IsType<string>(sentJson));
        var user = document
            .RootElement.GetProperty("mars")
            .GetProperty("reward")
            .GetProperty("payload")
            .GetProperty("user");

        Assert.Equal("Pyro", user.GetProperty("displayName").GetString());
        Assert.Equal("#FF0000", user.GetProperty("chatColor").GetString());
    }

    [Fact]
    public async Task AlertWithoutMediaIdIsSkipped()
    {
        var notifier = new Mock<ITelegramusNotifier>();

        await CreateNotifier(notifier.Object, Mock.Of<IMediaStorageClient>())
            .SendAlertAsync(Alert(Guid.Empty));

        Assert.Empty(notifier.Invocations);
    }

    [Fact]
    public async Task MissingMediaIsSkipped()
    {
        var notifier = new Mock<ITelegramusNotifier>();

        await CreateNotifier(notifier.Object, Mock.Of<IMediaStorageClient>())
            .SendAlertAsync(Alert(Guid.CreateVersion7()));

        Assert.Empty(notifier.Invocations);
    }

    private static HelloVideoNotifier CreateNotifier(
        ITelegramusNotifier notifier,
        IMediaStorageClient storage
    ) => new(notifier, storage, NullLogger<HelloVideoNotifier>.Instance);

    private static Mock<IMediaStorageClient> Storage(MediaInfo media)
    {
        var storage = new Mock<IMediaStorageClient>();
        storage
            .Setup(client =>
                client.GetMediaInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(media);

        return storage;
    }

    private static HelloVideoAlertDto Alert(
        Guid mediaInfoId,
        string displayName = "Pyro",
        string? chatColor = null
    ) =>
        new()
        {
            MediaInfoId = mediaInfoId,
            DisplayName = displayName,
            ChatColor = chatColor,
            Message = "привет",
        };

    private static MediaInfo Media() =>
        new()
        {
            TextInfo = new MediaTextInfo { Text = "привет" },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "/Alerts/hello.mp4",
                FileName = "hello.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "привет" },
            StylesInfo = new MediaStylesInfo(),
        };
}
