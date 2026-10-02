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

    /// <summary>
    /// Каждый метод нотификатора обязан отправить событие своего типа.
    ///
    /// Список из тридцати шести вызовов — не формальность: у события
    /// <c>oneof</c>, и ошибка в имени ветки не видна ни в компиляции, ни в
    /// типах, а оверлей просто не покажет событие. Таблица проверяет и то,
    /// что метод вообще что-то отправил, и то, какая именно ветка заполнена.
    /// </summary>
    [Fact]
    public async Task EveryNotifierMethodBroadcastsItsOwnEventCase()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();
        var waifu = new WaifuAlert { Id = Guid.NewGuid(), Name = "Remilia" };
        var husband = new HusbandAlert { Id = Guid.NewGuid(), DisplayName = "streamer" };
        var media = new MediaDto(
            new MediaInfo
            {
                TextInfo = new MediaTextInfo { Text = "текст" },
                FileInfo = new MediaFileInfo
                {
                    Type = MediaType.Image,
                    FilePath = "a.png",
                    FileName = "a.png",
                    Extension = "png",
                },
                PositionInfo = new MediaPositionInfo(),
                MetaInfo = new MediaMetaInfo { DisplayName = "viewer" },
                StylesInfo = new MediaStylesInfo(),
            }
        );
        var calls = new List<(
            string Name,
            Func<Task> Invoke,
            TelegramusEvent.EventOneofCase Expected
        )>
        {
            ("Alert", () => notifier.Alert(media), TelegramusEvent.EventOneofCase.Alert),
            ("Alerts", () => notifier.Alerts([media]), TelegramusEvent.EventOneofCase.Alerts),
            (
                "WaifuRoll",
                () => notifier.WaifuRoll(waifu, "streamer"),
                TelegramusEvent.EventOneofCase.WaifuRoll
            ),
            (
                "AddNewWaifu",
                () => notifier.AddNewWaifu(waifu, "streamer"),
                TelegramusEvent.EventOneofCase.AddNewWaifu
            ),
            (
                "ShowCurrentWife",
                () => notifier.ShowCurrentWife(waifu, husband),
                TelegramusEvent.EventOneofCase.ShowCurrentWife
            ),
            (
                "MergeWaifu",
                () => notifier.MergeWaifu(waifu, husband),
                TelegramusEvent.EventOneofCase.MergeWaifu
            ),
            (
                "UpdateWaifuPrizes",
                () => notifier.UpdateWaifuPrizes(new { Prizes = 1 }),
                TelegramusEvent.EventOneofCase.UpdateWaifuPrizes
            ),
            (
                "FumoFriday",
                () => notifier.FumoFriday("streamer"),
                TelegramusEvent.EventOneofCase.FumoFriday
            ),
            (
                "NewMessage",
                () => notifier.NewMessage("42", new { Text = "привет" }),
                TelegramusEvent.EventOneofCase.NewMessage
            ),
            (
                "DeleteMessage",
                () => notifier.DeleteMessage("42"),
                TelegramusEvent.EventOneofCase.DeleteMessage
            ),
            (
                "PostTwitchInfo",
                () => notifier.PostTwitchInfo("id", "secret"),
                TelegramusEvent.EventOneofCase.PostTwitchInfo
            ),
            (
                "MakeScreenParticles",
                () => notifier.MakeScreenParticles(new { Count = 5 }),
                TelegramusEvent.EventOneofCase.MakeScreenParticles
            ),
            (
                "MakeScreenEmojisParticles",
                () => notifier.MakeScreenEmojisParticles(new { Text = "★" }),
                TelegramusEvent.EventOneofCase.MakeScreenEmojisParticles
            ),
            (
                "RandomMem",
                () => notifier.RandomMem(media),
                TelegramusEvent.EventOneofCase.RandomMem
            ),
            (
                "AutoMessage",
                () => notifier.AutoMessage("сообщение"),
                TelegramusEvent.EventOneofCase.AutoMessage
            ),
            ("Adhd", () => notifier.Adhd(5), TelegramusEvent.EventOneofCase.Adhd),
            ("Explosion", () => notifier.Explosion(), TelegramusEvent.EventOneofCase.Explosion),
            ("LeroyAlert", () => notifier.LeroyAlert(), TelegramusEvent.EventOneofCase.LeroyAlert),
            (
                "GaoAlert",
                () => notifier.GaoAlert(new { Text = "гао" }),
                TelegramusEvent.EventOneofCase.GaoAlert
            ),
            ("Credits", () => notifier.Credits(), TelegramusEvent.EventOneofCase.Credits),
            (
                "MichaelJackson",
                () => notifier.MichaelJackson(),
                TelegramusEvent.EventOneofCase.MichaelJackson
            ),
            (
                "MikuMonday",
                () => notifier.MikuMonday(new { Prizes = 1 }),
                TelegramusEvent.EventOneofCase.MikuMonday
            ),
            ("PhonkEdit", () => notifier.PhonkEdit(), TelegramusEvent.EventOneofCase.PhonkEdit),
            (
                "TikTokEdit",
                () => notifier.TikTokEdit(Guid.NewGuid(), "челлендж"),
                TelegramusEvent.EventOneofCase.TikTokEdit
            ),
            (
                "AllRefund",
                () => notifier.AllRefund(new { Login = "pyro" }),
                TelegramusEvent.EventOneofCase.AllRefund
            ),
            (
                "AudioQuizStart",
                () => notifier.AudioQuizStart(new { Round = 1 }),
                TelegramusEvent.EventOneofCase.AudioQuizStart
            ),
            (
                "AudioQuizStop",
                () => notifier.AudioQuizStop(),
                TelegramusEvent.EventOneofCase.AudioQuizStop
            ),
            (
                "FumoRoll",
                () =>
                    notifier.FumoRoll(
                        new { Id = 1 },
                        new { Login = "pyro" },
                        collectedCount: 1,
                        totalCount: 2
                    ),
                TelegramusEvent.EventOneofCase.FumoRoll
            ),
            (
                "UpdateFumoPrizes",
                () => notifier.UpdateFumoPrizes(new { Prizes = 1 }),
                TelegramusEvent.EventOneofCase.UpdateFumoPrizes
            ),
            (
                "FrogRoll",
                () => notifier.FrogRoll(new { Id = 1 }, new { Login = "pyro" }),
                TelegramusEvent.EventOneofCase.FrogRoll
            ),
            (
                "UpdateFrogPrizes",
                () => notifier.UpdateFrogPrizes(new { Prizes = 1 }),
                TelegramusEvent.EventOneofCase.UpdateFrogPrizes
            ),
            (
                "MikuRoll",
                () =>
                    notifier.MikuRoll(
                        new { Id = 1 },
                        new { Login = "pyro" },
                        collectedCount: 1,
                        totalCount: 2
                    ),
                TelegramusEvent.EventOneofCase.MikuRoll
            ),
            (
                "UpdateMikuPrizes",
                () => notifier.UpdateMikuPrizes(new { Prizes = 1 }),
                TelegramusEvent.EventOneofCase.UpdateMikuPrizes
            ),
        };

        foreach (var (name, invoke, expected) in calls)
        {
            await invoke();

            var received = await ReadQueuedAsync(subscription);
            Assert.Equal(expected, received.EventCase);
            Assert.False(
                string.IsNullOrEmpty(received.ToString()),
                $"{name} отправил пустое событие."
            );
        }
    }

    /// <summary>
    /// Цвет и аватар не передаются как null: в протоколе это строки, и оверлей
    /// различает «цвет не задан» и «цвет пустой» по разному.
    /// </summary>
    [Fact]
    public async Task OptionalValuesBecomeEmptyStrings()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();
        var waifu = new WaifuAlert { Id = Guid.NewGuid(), Name = string.Empty };

        await notifier.WaifuRoll(waifu, "streamer", color: null);

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(string.Empty, received.WaifuRoll.Color);
        Assert.Equal(string.Empty, received.WaifuRoll.Waifu.Name);
        Assert.Equal(string.Empty, received.WaifuRoll.Waifu.ImageUrl);
    }

    [Fact]
    public async Task HusbandFieldsFallBackToEmptyStrings()
    {
        var broadcaster = new GrpcEventBroadcaster<TelegramusEvent>();
        var notifier = new TelegramusNotifier(broadcaster);
        using var subscription = broadcaster.Subscribe();

        await notifier.ShowCurrentWife(
            new WaifuAlert { Id = Guid.NewGuid(), Name = string.Empty },
            new HusbandAlert { Id = Guid.NewGuid(), DisplayName = string.Empty },
            avatar: null,
            color: null
        );

        var received = await ReadQueuedAsync(subscription);
        Assert.Equal(string.Empty, received.ShowCurrentWife.Husband.DisplayName);
        Assert.Equal(string.Empty, received.ShowCurrentWife.Husband.AvatarUrl);
        Assert.Equal(string.Empty, received.ShowCurrentWife.Avatar);
        Assert.Equal(string.Empty, received.ShowCurrentWife.Color);
    }
}
