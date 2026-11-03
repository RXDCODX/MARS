namespace MARS.Shared.Models.Rewards;

/// <summary>
/// Ключи алертов наград — контракт между бэкендом и оверлеем.
/// </summary>
/// <remarks>
/// <para>
/// Раньше маршрут строился из названия награды в Twitch
/// (<c>RabbitMqConfig.RewardKey(title)</c>). Названия русские и с эмодзи,
/// поэтому «🎉 Конфетти!» превращалось в <c>twitch.reward.конфетти</c>, тогда как
/// обработчик ждал <c>twitch.reward.confetti</c>, и совпадали 13 ключей из 33.
/// </para>
/// <para>
/// Теперь ключ хранится в данных награды (<c>ChannelRewardRecord.AlertKey</c>) и
/// публикуется как есть. Переименование награды в панели Twitch больше не ломает
/// маршрут. Значения — прежние имена без префикса <c>twitch.reward.</c>, чтобы
/// миграция данных была механической.
/// </para>
/// <para>
/// Список продублирован на клиенте
/// (<c>src/MARS.Gateway/ClientApp/src/shared/realtime/rewardAlertKeys.ts</c>),
/// расхождение ловит тест на обеих сторонах.
/// </para>
/// </remarks>
public static class RewardAlertKeys
{
    // Уже подключённые к оверлею награды.
    public const string Confetti = "confetti";
    public const string Fireworks = "fireworks";
    public const string PhonkEdit = "phonkedit";
    public const string TikTokEdit = "tiktokedit";
    public const string RandomArt = "randomart";
    public const string MikuMikuBeam = "mikumikubeam";
    public const string GaoAlert = "gaoalert";
    public const string MichaelTime = "michaeltime";
    public const string FumoFridayNight = "fumofridaynight";
    public const string MikuMonday = "mikumonday";
    public const string AdhdSuperpower = "adhdsuperpower";
    public const string CloseGame = "closegame";
    public const string Credits = "credits";
    public const string AllRefund = "allrefund";
    public const string WednsdayFrog = "wednsdayfrog";

    // Награды, у которых серверного эффекта нет: рендер целиком на клиенте.
    public const string What = "what";
    public const string Pedro = "pedro";
    public const string Tyazhelo = "tyazhelo";
    public const string LegBum = "legbum";
    public const string Stone = "stone";
    public const string Cringe = "cringe";
    public const string Hello = "hello";
    public const string Bye = "bye";
    public const string Intelligence = "intelligence";
    public const string MikuScreamer = "mikuscreamer";
    public const string Skibidibop = "skibidibop";
    public const string SkibidibopLong = "skibidiboplong";
    public const string Aga = "aga";
    public const string BadToBone = "badtobone";
    public const string DanceDance = "dancedance";
    public const string Edge0100Alert = "edge0100alert";
    public const string StatusQuestion = "statusquestion";
    public const string CinemaRequest = "cinemarequest";
    public const string SelectGame = "selectgame";

    // Роллы и вайфу-награды: раньше маршрута не было вовсе.
    public const string FrogRoll = "frogroll";
    public const string FumoRoll = "fumoroll";
    public const string MikuRoll = "mikuroll";
    public const string WaifuRoll = "waifuroll";
    public const string WaifuAdd = "waifuadd";
    public const string WaifuMerge = "waifumerge";
    public const string WaifuShow = "waifushow";
    public const string FumoFriday = "fumofriday";
    public const string MikuMondayTracks = "mikumondaytracks";
    public const string RandomMem = "randommem";
    public const string Emojis = "emojis";
    public const string RandomSound = "randomsound";
    public const string RussianRoulette = "russianroulette";
    public const string Quiz = "quiz";
    public const string RandomReward = "randomreward";
    public const string AudioQuiz = "audioquiz";

    /// <summary>Приветственное видео: не награда Twitch, но едет тем же потоком.</summary>
    public const string HelloVideo = "hellovideo";

    /// <summary>Все известные ключи. Используется тестами и админкой.</summary>
    public static readonly string[] All =
    [
        Confetti,
        Fireworks,
        PhonkEdit,
        TikTokEdit,
        RandomArt,
        MikuMikuBeam,
        GaoAlert,
        MichaelTime,
        FumoFridayNight,
        MikuMonday,
        AdhdSuperpower,
        CloseGame,
        Credits,
        AllRefund,
        WednsdayFrog,
        What,
        Pedro,
        Tyazhelo,
        LegBum,
        Stone,
        Cringe,
        Hello,
        Bye,
        Intelligence,
        MikuScreamer,
        Skibidibop,
        SkibidibopLong,
        Aga,
        BadToBone,
        DanceDance,
        Edge0100Alert,
        StatusQuestion,
        CinemaRequest,
        SelectGame,
        FrogRoll,
        FumoRoll,
        MikuRoll,
        WaifuRoll,
        WaifuAdd,
        WaifuMerge,
        WaifuShow,
        FumoFriday,
        MikuMondayTracks,
        RandomMem,
        Emojis,
        RandomSound,
        RussianRoulette,
        Quiz,
        RandomReward,
        AudioQuiz,
        HelloVideo,
    ];
}
