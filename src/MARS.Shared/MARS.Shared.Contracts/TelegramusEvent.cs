using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

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

    /// <summary>
    /// Имя заполненной ветки. Раньше это был enum с ручной цепочкой из 36 проверок
    /// и дублирующимся списком имён; теперь имя берётся из тех же свойств, по
    /// которым работает диспетчер.
    /// </summary>
    public string? EventCase => TelegramusEventContract.GetActiveBranch(this)?.Name;
}
