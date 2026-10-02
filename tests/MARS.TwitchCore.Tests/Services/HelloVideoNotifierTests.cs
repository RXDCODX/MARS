using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using MARS.TwitchCore.Services.HelloVideos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Нотификатор приветственного видео.
///
/// Видео уходит в общий поток наград с уже подставленными пользователем и медиа.
/// Проверяется, что без медиа событие не публикуется: иначе OBS получил бы
/// оповещение без файла и показал бы пустую плашку.
/// </summary>
public class HelloVideoNotifierTests
{
    [Fact]
    public async Task AlertIsPublishedWithMedia()
    {
        var media = Media();
        var bus = new Mock<IMarsEventBus>();
        var storage = new Mock<IMediaStorageClient>();
        storage
            .Setup(client =>
                client.GetMediaInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(media);

        await new HelloVideoNotifier(
            bus.Object,
            storage.Object,
            NullLogger<HelloVideoNotifier>.Instance
        ).SendAlertAsync(Alert(media.Id));

        bus.Verify(
            publisher =>
                publisher.PublishAsync(
                    RabbitMqConfig.RewardRedeemed,
                    It.Is<RewardRedeemedEvent>(rewardEvent => rewardEvent.Media == media),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// В событии подставляется автор приветствия: OBS подписывает оповещение его
    /// именем.
    /// </summary>
    [Fact]
    public async Task AuthorIsAttachedToTheEvent()
    {
        var media = Media();
        var bus = new Mock<IMarsEventBus>();
        var storage = new Mock<IMediaStorageClient>();
        storage
            .Setup(client =>
                client.GetMediaInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(media);
        var alert = Alert(media.Id, "Pyro", "#FF0000");

        await new HelloVideoNotifier(
            bus.Object,
            storage.Object,
            NullLogger<HelloVideoNotifier>.Instance
        ).SendAlertAsync(alert);

        bus.Verify(
            publisher =>
                publisher.PublishAsync(
                    RabbitMqConfig.RewardRedeemed,
                    It.Is<RewardRedeemedEvent>(rewardEvent =>
                        rewardEvent.UserName == "Pyro"
                        && rewardEvent.User != null
                        && rewardEvent.User.DisplayName == "Pyro"
                        && rewardEvent.User.ChatColor == "#FF0000"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Без идентификатора медиа событие не уходит: файла нет, показывать нечего.
    /// </summary>
    [Fact]
    public async Task AlertWithoutMediaIdIsSkipped()
    {
        var bus = new Mock<IMarsEventBus>();

        await new HelloVideoNotifier(
            bus.Object,
            Mock.Of<IMediaStorageClient>(),
            NullLogger<HelloVideoNotifier>.Instance
        ).SendAlertAsync(Alert(Guid.Empty));

        Assert.Empty(bus.Invocations);
    }

    /// <summary>
    /// Медиа, которого нет в хранилище, не публикуется: событие без файла показало бы
    /// зрителю пустую плашку.
    /// </summary>
    [Fact]
    public async Task MissingMediaIsSkipped()
    {
        var bus = new Mock<IMarsEventBus>();

        await new HelloVideoNotifier(
            bus.Object,
            Mock.Of<IMediaStorageClient>(),
            NullLogger<HelloVideoNotifier>.Instance
        ).SendAlertAsync(Alert(Guid.CreateVersion7()));

        Assert.Empty(bus.Invocations);
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
