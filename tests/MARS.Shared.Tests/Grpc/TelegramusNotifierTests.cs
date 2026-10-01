using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Grpc.Telegramus;
using MARS.Shared.Models.Media;

namespace MARS.Shared.Tests.Grpc;

public class TelegramusNotifierTests
{
    [Fact]
    public async Task Alert_SendsTypedMediaToSubscribers()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();
        var dto = new MediaDto(
            new MediaInfo
            {
                TextInfo = new MediaTextInfo { Text = "алерт" },
                FileInfo = new MediaFileInfo
                {
                    Type = MediaType.Image,
                    FilePath = "Alerts/alert.png",
                    FileName = "alert.png",
                    Extension = "png",
                },
                PositionInfo = new MediaPositionInfo(),
                MetaInfo = new MediaMetaInfo { DisplayName = "viewer" },
                StylesInfo = new MediaStylesInfo(),
            }
        );

        await notifier.Alert(dto);

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(TelegramusEvent.EventOneofCase.Alert, received.EventCase);
        Assert.Equal("алерт", received.Alert.Media.MediaInfo.TextInfo.Text);
        Assert.Equal("Alerts/alert.png", received.Alert.Media.MediaInfo.FileInfo.FilePath);
    }

    [Fact]
    public async Task WaifuRoll_SendsWaifuAndDisplayName()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();
        var waifu = new WaifuAlert
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Name = "Remilia",
            ImageUrl = "https://remilia.flare",
        };

        await notifier.WaifuRoll(waifu, "streamer", "#ff0000");

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(TelegramusEvent.EventOneofCase.WaifuRoll, received.EventCase);
        Assert.Equal("Remilia", received.WaifuRoll.Waifu.Name);
        Assert.Equal("streamer", received.WaifuRoll.DisplayName);
        Assert.Equal("#ff0000", received.WaifuRoll.Color);
    }

    [Fact]
    public async Task Highlite_KeepsJsonPayloadOfObjectArgument()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();

        await notifier.Highlite(
            new { UserId = "42", Nick = "pyro" },
            "#00ff00",
            new { Url = "https://face" }
        );

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(TelegramusEvent.EventOneofCase.Highlite, received.EventCase);
        Assert.Equal(
            "{\"userId\":\"42\",\"nick\":\"pyro\"}",
            received.Highlite.MessageJson.ToStringUtf8()
        );
        Assert.Equal("{\"url\":\"https://face\"}", received.Highlite.FaceUrlJson.ToStringUtf8());
        Assert.Equal("#00ff00", received.Highlite.Color);
    }

    [Fact]
    public async Task MikuMikuBeam_SerializesEveryUserSeparately()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();

        await notifier.MikuMikuBeam([new { Login = "a" }, new { Login = "b" }]);

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(TelegramusEvent.EventOneofCase.MikuMikuBeam, received.EventCase);
        Assert.Equal(2, received.MikuMikuBeam.UsersJson.Count);
        Assert.Equal("{\"login\":\"a\"}", received.MikuMikuBeam.UsersJson[0].ToStringUtf8());
        Assert.Equal("{\"login\":\"b\"}", received.MikuMikuBeam.UsersJson[1].ToStringUtf8());
    }

    [Fact]
    public async Task TikTokEdit_FormatsGuidAsString()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();
        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await notifier.TikTokEdit(guid, "челлендж");

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(TelegramusEvent.EventOneofCase.TikTokEdit, received.EventCase);
        Assert.Equal(guid.ToString(), received.TikTokEdit.Guid);
        Assert.Equal("челлендж", received.TikTokEdit.Text);
    }

    [Fact]
    public async Task Explosion_SendsEventWithoutPayload()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();

        await notifier.Explosion();

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(TelegramusEvent.EventOneofCase.Explosion, received.EventCase);
    }

    private static async Task<TelegramusEvent> ReadQueuedAsync(
        GrpcSubscription<TelegramusEvent> subscription
    )
    {
        return await subscription.Queue.Reader.ReadAsync(TestContext.Current.CancellationToken);
    }
}
