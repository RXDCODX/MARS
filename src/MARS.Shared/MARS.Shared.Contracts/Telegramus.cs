using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class WaifuInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Source { get; set; } = "";
}

public class HusbandInfo
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
}

public class AlertEvent
{
    public MediaPayload? Media { get; set; }
}

public class AlertsEvent
{
    public List<MediaPayload> Media { get; set; } = new();
}

public class WaifuRollEvent
{
    public WaifuInfo? Waifu { get; set; }
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}

public class AddNewWaifuEvent
{
    public WaifuInfo? Waifu { get; set; }
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}

public class ShowCurrentWifeEvent
{
    public WaifuInfo? Waifu { get; set; }
    public HusbandInfo? Husband { get; set; }
    public string Avatar { get; set; } = "";
    public string Color { get; set; } = "";
}

public class MergeWaifuEvent
{
    public WaifuInfo? Waifu { get; set; }
    public HusbandInfo? Husband { get; set; }
    public string Avatar { get; set; } = "";
    public string Color { get; set; } = "";
}

public class PrizesEvent
{
    public string PrizesJson { get; set; } = "";
}

public class FumoFridayEvent
{
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}

public class NewMessageEvent
{
    public string Id { get; set; } = "";
    public string MessageJson { get; set; } = "";
}

public class DeleteMessageEvent
{
    public string Id { get; set; } = "";
}

public class HighliteEvent
{
    public string MessageJson { get; set; } = "";
    public string Color { get; set; } = "";
    public string FaceUrlJson { get; set; } = "";
}

public class PostTwitchInfoEvent
{
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
}

public class ScreenParticlesEvent
{
    public string ParticlesJson { get; set; } = "";
}

public class ScreenEmojisParticlesEvent
{
    public string MessageJson { get; set; } = "";
}

public class AutoMessageEvent
{
    public string Message { get; set; } = "";
}

public class AdhdEvent
{
    public int Seconds { get; set; }
}

public class AdhdConfigEvent
{
    public string ConfigJson { get; set; } = "";
}

public class EmptyEvent { }

public class GaoAlertEvent
{
    public string GaoAlertJson { get; set; } = "";
}

public class MikuMondayEvent
{
    public string MikuMondayJson { get; set; } = "";
}

public class MikuMikuBeamEvent
{
    public List<string> UsersJson { get; set; } = new();
}

public class TikTokEditEvent
{
    public string Guid { get; set; } = "";
    public string Text { get; set; } = "";
}

public class AllRefundEvent
{
    public string UserJson { get; set; } = "";
}

public class AudioQuizStartEvent
{
    public string RoundJson { get; set; } = "";
}

public class FumoRollEvent
{
    public string FumoJson { get; set; } = "";
    public string TwitchUserJson { get; set; } = "";
    public string Color { get; set; } = "";
    public int CollectedCount { get; set; }
    public int TotalCount { get; set; }
}

public class FrogRollEvent
{
    public string FrogJson { get; set; } = "";
    public string TwitchUserJson { get; set; } = "";
    public string Color { get; set; } = "";
}

public class MikuRollEvent
{
    public string MikuModuleJson { get; set; } = "";
    public string TwitchUserJson { get; set; } = "";
    public string Color { get; set; } = "";
    public int CollectedCount { get; set; }
    public int TotalCount { get; set; }
}

public class TelegramusEvent
{
    public AlertEvent? Alert { get; set; }
    public AlertsEvent? Alerts { get; set; }
    public WaifuRollEvent? WaifuRoll { get; set; }
    public AddNewWaifuEvent? AddNewWaifu { get; set; }
    public ShowCurrentWifeEvent? ShowCurrentWife { get; set; }
    public MergeWaifuEvent? MergeWaifu { get; set; }
    public PrizesEvent? UpdateWaifuPrizes { get; set; }
    public FumoFridayEvent? FumoFriday { get; set; }
    public NewMessageEvent? NewMessage { get; set; }
    public DeleteMessageEvent? DeleteMessage { get; set; }
    public HighliteEvent? Highlite { get; set; }
    public PostTwitchInfoEvent? PostTwitchInfo { get; set; }
    public ScreenParticlesEvent? MakeScreenParticles { get; set; }
    public ScreenEmojisParticlesEvent? MakeScreenEmojisParticles { get; set; }
    public AlertEvent? RandomMem { get; set; }
    public AutoMessageEvent? AutoMessage { get; set; }
    public AdhdEvent? Adhd { get; set; }
    public EmptyEvent? Explosion { get; set; }
    public EmptyEvent? LeroyAlert { get; set; }
    public GaoAlertEvent? GaoAlert { get; set; }
    public EmptyEvent? Credits { get; set; }
    public EmptyEvent? MichaelJackson { get; set; }
    public MikuMondayEvent? MikuMonday { get; set; }
    public MikuMikuBeamEvent? MikuMikuBeam { get; set; }
    public EmptyEvent? PhonkEdit { get; set; }
    public TikTokEditEvent? TikTokEdit { get; set; }
    public AllRefundEvent? AllRefund { get; set; }
    public AudioQuizStartEvent? AudioQuizStart { get; set; }
    public EmptyEvent? AudioQuizStop { get; set; }
    public FumoRollEvent? FumoRoll { get; set; }
    public PrizesEvent? UpdateFumoPrizes { get; set; }
    public FrogRollEvent? FrogRoll { get; set; }
    public PrizesEvent? UpdateFrogPrizes { get; set; }
    public MikuRollEvent? MikuRoll { get; set; }
    public PrizesEvent? UpdateMikuPrizes { get; set; }
    public AdhdConfigEvent? AdhdConfig { get; set; }

    public enum EventOneofCase
    {
        None = 0,
        Alert = 1,
        Alerts = 2,
        WaifuRoll = 3,
        AddNewWaifu = 4,
        ShowCurrentWife = 5,
        MergeWaifu = 6,
        UpdateWaifuPrizes = 7,
        FumoFriday = 8,
        NewMessage = 9,
        DeleteMessage = 10,
        Highlite = 11,
        PostTwitchInfo = 12,
        MakeScreenParticles = 13,
        MakeScreenEmojisParticles = 14,
        RandomMem = 15,
        AutoMessage = 16,
        Adhd = 17,
        Explosion = 18,
        LeroyAlert = 19,
        GaoAlert = 20,
        Credits = 21,
        MichaelJackson = 22,
        MikuMonday = 23,
        MikuMikuBeam = 24,
        PhonkEdit = 25,
        TikTokEdit = 26,
        AllRefund = 27,
        AudioQuizStart = 28,
        AudioQuizStop = 29,
        FumoRoll = 30,
        UpdateFumoPrizes = 31,
        FrogRoll = 32,
        UpdateFrogPrizes = 33,
        MikuRoll = 34,
        UpdateMikuPrizes = 35,
        AdhdConfig = 36,
    }

    public EventOneofCase EventCase
    {
        get
        {
            if (Alert is not null)
                return EventOneofCase.Alert;
            if (Alerts is not null)
                return EventOneofCase.Alerts;
            if (WaifuRoll is not null)
                return EventOneofCase.WaifuRoll;
            if (AddNewWaifu is not null)
                return EventOneofCase.AddNewWaifu;
            if (ShowCurrentWife is not null)
                return EventOneofCase.ShowCurrentWife;
            if (MergeWaifu is not null)
                return EventOneofCase.MergeWaifu;
            if (UpdateWaifuPrizes is not null)
                return EventOneofCase.UpdateWaifuPrizes;
            if (FumoFriday is not null)
                return EventOneofCase.FumoFriday;
            if (NewMessage is not null)
                return EventOneofCase.NewMessage;
            if (DeleteMessage is not null)
                return EventOneofCase.DeleteMessage;
            if (Highlite is not null)
                return EventOneofCase.Highlite;
            if (PostTwitchInfo is not null)
                return EventOneofCase.PostTwitchInfo;
            if (MakeScreenParticles is not null)
                return EventOneofCase.MakeScreenParticles;
            if (MakeScreenEmojisParticles is not null)
                return EventOneofCase.MakeScreenEmojisParticles;
            if (RandomMem is not null)
                return EventOneofCase.RandomMem;
            if (AutoMessage is not null)
                return EventOneofCase.AutoMessage;
            if (Adhd is not null)
                return EventOneofCase.Adhd;
            if (Explosion is not null)
                return EventOneofCase.Explosion;
            if (LeroyAlert is not null)
                return EventOneofCase.LeroyAlert;
            if (GaoAlert is not null)
                return EventOneofCase.GaoAlert;
            if (Credits is not null)
                return EventOneofCase.Credits;
            if (MichaelJackson is not null)
                return EventOneofCase.MichaelJackson;
            if (MikuMonday is not null)
                return EventOneofCase.MikuMonday;
            if (MikuMikuBeam is not null)
                return EventOneofCase.MikuMikuBeam;
            if (PhonkEdit is not null)
                return EventOneofCase.PhonkEdit;
            if (TikTokEdit is not null)
                return EventOneofCase.TikTokEdit;
            if (AllRefund is not null)
                return EventOneofCase.AllRefund;
            if (AudioQuizStart is not null)
                return EventOneofCase.AudioQuizStart;
            if (AudioQuizStop is not null)
                return EventOneofCase.AudioQuizStop;
            if (FumoRoll is not null)
                return EventOneofCase.FumoRoll;
            if (UpdateFumoPrizes is not null)
                return EventOneofCase.UpdateFumoPrizes;
            if (FrogRoll is not null)
                return EventOneofCase.FrogRoll;
            if (UpdateFrogPrizes is not null)
                return EventOneofCase.UpdateFrogPrizes;
            if (MikuRoll is not null)
                return EventOneofCase.MikuRoll;
            if (UpdateMikuPrizes is not null)
                return EventOneofCase.UpdateMikuPrizes;
            if (AdhdConfig is not null)
                return EventOneofCase.AdhdConfig;
            return EventOneofCase.None;
        }
    }
}
