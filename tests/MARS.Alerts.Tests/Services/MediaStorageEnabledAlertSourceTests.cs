using MARS.Alerts.Services.Alerts;
using MARS.Shared.Clients;
using MARS.Shared.Models.Media;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Источник алертов из MARS.MediaStorage.
///
/// Проверяется отсев выключенных и безымённых алертов: список из всех записей
/// базы показывать нельзя, в оверлей попадают только включённые с командой.
/// </summary>
public class MediaStorageEnabledAlertSourceTests
{
    private readonly Mock<IMediaStorageClient> _client = new();

    [Fact]
    public async Task OnlyEnabledAlertsWithTriggerAreReturned()
    {
        _client
            .Setup(client => client.GetAllAlertsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([Enabled("аяка", isEnabled: true), Enabled("сава", isEnabled: false)]);

        var alerts = await Create().GetEnabledAlertsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(alerts);
        Assert.Equal(["аяка"], alerts!.Select(alert => alert.MetaInfo.DisplayName));
    }

    /// <summary>
    /// Алерт без триггерного слова не запускается командой — в списке он лишний.
    /// </summary>
    [Fact]
    public async Task AlertWithoutTriggerWordIsSkipped()
    {
        _client
            .Setup(client => client.GetAllAlertsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([Enabled("аяка", isEnabled: true, triggerWord: "  ")]);

        var alerts = await Create().GetEnabledAlertsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(alerts!);
    }

    /// <summary>
    /// null от клиента означает «хранилище недоступно», и это отличается от пустого
    /// списка, поэтому наружу отдаётся null, а не пустой массив.
    /// </summary>
    [Fact]
    public async Task UnavailableStorageYieldsNull()
    {
        _client
            .Setup(client => client.GetAllAlertsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync((IReadOnlyList<MediaInfo>?)null);

        Assert.Null(await Create().GetEnabledAlertsAsync(TestContext.Current.CancellationToken));
    }

    private MediaStorageEnabledAlertSource Create() => new(_client.Object);

    private static MediaInfo Enabled(string name, bool isEnabled, string triggerWord = "аяка") =>
        new()
        {
            TextInfo = new MediaTextInfo { TriggerWord = triggerWord },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Audio,
                FilePath = "memory/аяка.mp3",
                FileName = "аяка",
                Extension = ".mp3",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = name, IsEnabled = isEnabled },
            StylesInfo = new MediaStylesInfo(),
        };
}
