using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Telegramus;
using MARS.Shared.Models.Media;
using MARS.Shared.Telemetry;

namespace MARS.Shared.Grpc.Notifications;

public sealed class TelegramusNotifier(ITelegramusEventSink sink) : ITelegramusNotifier
{
    public Task Alert(MediaDto info)
    {
        return BroadcastAsync(
            new TelegramusEvent { Alert = new AlertEvent { Media = MediaGrpcMapper.ToProto(info) } }
        );
    }

    public Task Alerts(MediaDto[] info)
    {
        var alerts = new AlertsEvent();
        alerts.Media.AddRange([.. info.Select(MediaGrpcMapper.ToProto)]);

        return BroadcastAsync(new TelegramusEvent { Alerts = alerts });
    }

    public Task WaifuRoll(WaifuAlert content, string displayName, string? color = null)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                WaifuRoll = new WaifuRollEvent
                {
                    Waifu = ToProto(content),
                    DisplayName = displayName,
                    Color = color ?? string.Empty,
                },
            }
        );
    }

    public Task AddNewWaifu(WaifuAlert content, string displayName, string? color = null)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                AddNewWaifu = new AddNewWaifuEvent
                {
                    Waifu = ToProto(content),
                    DisplayName = displayName,
                    Color = color ?? string.Empty,
                },
            }
        );
    }

    public Task ShowCurrentWife(
        WaifuAlert content,
        HusbandAlert husband,
        string? avatar = null,
        string? color = null
    )
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                ShowCurrentWife = new ShowCurrentWifeEvent
                {
                    Waifu = ToProto(content),
                    Husband = ToProto(husband),
                    Avatar = avatar ?? string.Empty,
                    Color = color ?? string.Empty,
                },
            }
        );
    }

    public Task MergeWaifu(
        WaifuAlert content,
        HusbandAlert husband,
        string? avatar = null,
        string? color = null
    )
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                MergeWaifu = new MergeWaifuEvent
                {
                    Waifu = ToProto(content),
                    Husband = ToProto(husband),
                    Avatar = avatar ?? string.Empty,
                    Color = color ?? string.Empty,
                },
            }
        );
    }

    public Task UpdateWaifuPrizes(object prizes)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                UpdateWaifuPrizes = new PrizesEvent { PrizesJson = MarsGrpcJson.Serialize(prizes) },
            }
        );
    }

    public Task FumoFriday(string displayName, string? color = null)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                FumoFriday = new FumoFridayEvent
                {
                    DisplayName = displayName,
                    Color = color ?? string.Empty,
                },
            }
        );
    }

    public Task NewMessage(string id, object message)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                NewMessage = new NewMessageEvent
                {
                    Id = id,
                    MessageJson = MarsGrpcJson.Serialize(message),
                },
            }
        );
    }

    public Task DeleteMessage(string id)
    {
        return BroadcastAsync(
            new TelegramusEvent { DeleteMessage = new DeleteMessageEvent { Id = id } }
        );
    }

    public Task Highlite(object message, string color, object faceUrl)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                Highlite = new HighliteEvent
                {
                    MessageJson = MarsGrpcJson.Serialize(message),
                    Color = color,
                    FaceUrlJson = MarsGrpcJson.Serialize(faceUrl),
                },
            }
        );
    }

    public Task PostTwitchInfo(string clientId, string secret)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                PostTwitchInfo = new PostTwitchInfoEvent { ClientId = clientId, Secret = secret },
            }
        );
    }

    public Task MakeScreenParticles(object particles)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                MakeScreenParticles = new ScreenParticlesEvent
                {
                    ParticlesJson = MarsGrpcJson.Serialize(particles),
                },
            }
        );
    }

    public Task MakeScreenEmojisParticles(object message)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                MakeScreenEmojisParticles = new ScreenEmojisParticlesEvent
                {
                    MessageJson = MarsGrpcJson.Serialize(message),
                },
            }
        );
    }

    public Task RandomMem(MediaDto mediaInfo)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                RandomMem = new AlertEvent { Media = MediaGrpcMapper.ToProto(mediaInfo) },
            }
        );
    }

    public Task AutoMessage(string message)
    {
        return BroadcastAsync(
            new TelegramusEvent { AutoMessage = new AutoMessageEvent { Message = message } }
        );
    }

    public Task Adhd(int seconds)
    {
        return BroadcastAsync(new TelegramusEvent { Adhd = new AdhdEvent { Seconds = seconds } });
    }

    public Task Explosion()
    {
        return BroadcastAsync(new TelegramusEvent { Explosion = new EmptyEvent() });
    }

    public Task LeroyAlert()
    {
        return BroadcastAsync(new TelegramusEvent { LeroyAlert = new EmptyEvent() });
    }

    public Task GaoAlert(object gaoAlert)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                GaoAlert = new GaoAlertEvent { GaoAlertJson = MarsGrpcJson.Serialize(gaoAlert) },
            }
        );
    }

    public Task Credits()
    {
        return BroadcastAsync(new TelegramusEvent { Credits = new EmptyEvent() });
    }

    public Task MichaelJackson()
    {
        return BroadcastAsync(new TelegramusEvent { MichaelJackson = new EmptyEvent() });
    }

    public Task MikuMonday(object mikuMondayData)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                MikuMonday = new MikuMondayEvent
                {
                    MikuMondayJson = MarsGrpcJson.Serialize(mikuMondayData),
                },
            }
        );
    }

    public Task MikuMikuBeam(List<object> users)
    {
        var beam = new MikuMikuBeamEvent();
        beam.UsersJson.AddRange([.. users.Select(MarsGrpcJson.Serialize)]);

        return BroadcastAsync(new TelegramusEvent { MikuMikuBeam = beam });
    }

    public Task PhonkEdit()
    {
        return BroadcastAsync(new TelegramusEvent { PhonkEdit = new EmptyEvent() });
    }

    public Task TikTokEdit(Guid guid, string text)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                TikTokEdit = new TikTokEditEvent { Guid = guid.ToString(), Text = text },
            }
        );
    }

    public Task AllRefund(object user)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                AllRefund = new AllRefundEvent { UserJson = MarsGrpcJson.Serialize(user) },
            }
        );
    }

    public Task AudioQuizStart(object round)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                AudioQuizStart = new AudioQuizStartEvent
                {
                    RoundJson = MarsGrpcJson.Serialize(round),
                },
            }
        );
    }

    public Task AudioQuizStop()
    {
        return BroadcastAsync(new TelegramusEvent { AudioQuizStop = new EmptyEvent() });
    }

    public Task FumoRoll(
        object fumo,
        object twitchUser,
        string? color = null,
        int collectedCount = 0,
        int totalCount = 0
    )
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                FumoRoll = new FumoRollEvent
                {
                    FumoJson = MarsGrpcJson.Serialize(fumo),
                    TwitchUserJson = MarsGrpcJson.Serialize(twitchUser),
                    Color = color ?? string.Empty,
                    CollectedCount = collectedCount,
                    TotalCount = totalCount,
                },
            }
        );
    }

    public Task UpdateFumoPrizes(object prizes)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                UpdateFumoPrizes = new PrizesEvent { PrizesJson = MarsGrpcJson.Serialize(prizes) },
            }
        );
    }

    public Task FrogRoll(object frog, object twitchUser, string? color = null)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                FrogRoll = new FrogRollEvent
                {
                    FrogJson = MarsGrpcJson.Serialize(frog),
                    TwitchUserJson = MarsGrpcJson.Serialize(twitchUser),
                    Color = color ?? string.Empty,
                },
            }
        );
    }

    public Task UpdateFrogPrizes(object prizes)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                UpdateFrogPrizes = new PrizesEvent { PrizesJson = MarsGrpcJson.Serialize(prizes) },
            }
        );
    }

    public Task MikuRoll(
        object mikuModule,
        object twitchUser,
        string? color = null,
        int collectedCount = 0,
        int totalCount = 0
    )
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                MikuRoll = new MikuRollEvent
                {
                    MikuModuleJson = MarsGrpcJson.Serialize(mikuModule),
                    TwitchUserJson = MarsGrpcJson.Serialize(twitchUser),
                    Color = color ?? string.Empty,
                    CollectedCount = collectedCount,
                    TotalCount = totalCount,
                },
            }
        );
    }

    public Task UpdateMikuPrizes(object prizes)
    {
        return BroadcastAsync(
            new TelegramusEvent
            {
                UpdateMikuPrizes = new PrizesEvent { PrizesJson = MarsGrpcJson.Serialize(prizes) },
            }
        );
    }

    private static WaifuInfo ToProto(WaifuAlert waifu)
    {
        return new WaifuInfo
        {
            Id = waifu.Id.ToString(),
            Name = waifu.Name ?? string.Empty,
            ImageUrl = waifu.ImageUrl ?? string.Empty,
            Source = waifu.Source ?? string.Empty,
        };
    }

    private static HusbandInfo ToProto(HusbandAlert husband)
    {
        return new HusbandInfo
        {
            Id = husband.Id.ToString(),
            DisplayName = husband.DisplayName ?? string.Empty,
            AvatarUrl = husband.AvatarUrl ?? string.Empty,
        };
    }

    private Task BroadcastAsync(TelegramusEvent notification)
    {
        return sink.DispatchAsync(notification);
    }
}
