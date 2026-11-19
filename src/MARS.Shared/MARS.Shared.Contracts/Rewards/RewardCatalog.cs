using System.Linq;

namespace MARS.Shared.Contracts.Rewards;

public static class RewardCatalog
{
    public static IReadOnlyList<RewardDefinition> All { get; } =
    [
        new()
        {
            Code = "random_reward",
            ExpectedCost = 1,
            NormalizedEmoji = "🎲",
            LegacyTitle = "🎲 Случайная награда!",
            UserCooldown = TimeSpan.FromSeconds(60),
            ExcludedFromRandomPool = true,
        },
        new()
        {
            Code = "waifu_marriage",
            ExpectedCost = 2,
            NormalizedEmoji = "💒",
            LegacyTitle = "💒 Свадьба",
        },
        new()
        {
            Code = "search_wife",
            ExpectedCost = 4,
            NormalizedEmoji = "🔍",
            LegacyTitle = "🔍 Поиск супруга",
            Group = "roll4",
        },
        new()
        {
            Code = "miku_roulette",
            ExpectedCost = 4,
            NormalizedEmoji = "🎀",
            LegacyTitle = "🎀 Мику Рулетка",
            Group = "roll4",
        },
        new()
        {
            Code = "frog_roulette",
            ExpectedCost = 4,
            NormalizedEmoji = "🐸",
            LegacyTitle = "🐸 Frog Roulette",
            Group = "roll4",
        },
        new()
        {
            Code = "fumo_roulette",
            ExpectedCost = 4,
            NormalizedEmoji = "🧸",
            LegacyTitle = "🧸 Fumo Roulette",
            Group = "roll4",
        },
        new()
        {
            Code = "add_wife",
            ExpectedCost = 5,
            NormalizedEmoji = "👰",
            LegacyTitle = "👰 Добавить супруга",
            RequiresUserInput = true,
        },
        new()
        {
            Code = "russian_roulette",
            ExpectedCost = 6,
            NormalizedEmoji = "🔫",
            LegacyTitle = "🔫 Русская рулетка",
        },
        new()
        {
            Code = "quiz",
            ExpectedCost = 7,
            NormalizedEmoji = "🧠",
            LegacyTitle = "🧠 Викторина",
        },
        new()
        {
            Code = "audio_quiz",
            ExpectedCost = 9,
            NormalizedEmoji = "🎵",
            LegacyTitle = "🎵 Аудио викторина",
        },
        new()
        {
            Code = "random_sound",
            ExpectedCost = 10,
            NormalizedEmoji = "🔊",
            LegacyTitle = "🔊 Random sound",
            RequiresFollower = true,
        },
        new()
        {
            Code = "random_mem",
            ExpectedCost = 11,
            NormalizedEmoji = "😂",
            LegacyTitle = "😂 Random Mem",
        },
        new()
        {
            Code = "fumo_friday",
            ExpectedCost = 13,
            NormalizedEmoji = "🌙",
            LegacyTitle = "🌙 Fumo Friday",
        },
        new()
        {
            Code = "gao_alert",
            ExpectedCost = 18,
            NormalizedEmoji = "🦁",
            LegacyTitle = "🦁 GAO Alert",
        },
        new()
        {
            Code = "random_art",
            ExpectedCost = 27,
            NormalizedEmoji = "🎨",
            LegacyTitle = "🎨 Random Art",
        },
        new()
        {
            Code = "wednesday_frog",
            ExpectedCost = 38,
            NormalizedEmoji = "🐸",
            LegacyTitle = "🐸 Жабья среда",
        },
        new()
        {
            Code = "miku_monday",
            ExpectedCost = 39,
            NormalizedEmoji = "🎤",
            LegacyTitle = "🎤 Miku Monday [BETA TEST]",
        },
        new()
        {
            Code = "what",
            ExpectedCost = 61,
            NormalizedEmoji = "❓",
            LegacyTitle = "❓ WHAT",
        },
        new()
        {
            Code = "pedro",
            ExpectedCost = 134,
            NormalizedEmoji = "👻",
            LegacyTitle = "👻 PEDRO PEDRO PEDRO",
        },
        new()
        {
            Code = "tyazhelo",
            ExpectedCost = 150,
            NormalizedEmoji = "😩",
            LegacyTitle = "😩 Тяжело...",
        },
        new()
        {
            Code = "michael_time",
            ExpectedCost = 155,
            NormalizedEmoji = "⏰",
            LegacyTitle = "⏰ Michael Time!",
        },
        new()
        {
            Code = "leg_bum",
            ExpectedCost = 160,
            NormalizedEmoji = "🦵",
            LegacyTitle = "\uFFFD발 НОГОЙ БОМЖА",
            RequiresUserInput = true,
        },
        new()
        {
            Code = "fumo_friday_night",
            ExpectedCost = 170,
            NormalizedEmoji = "🧸",
            LegacyTitle = "🧸 Fumo Friday Night",
            Group = "day170",
        },
        new()
        {
            Code = "miku_monday_alert",
            ExpectedCost = 170,
            NormalizedEmoji = "🎤",
            LegacyTitle = "🎤 Miku Monday Alert",
            Group = "day170",
        },
        new()
        {
            Code = "stone",
            ExpectedCost = 182,
            NormalizedEmoji = "🪨",
            LegacyTitle = "🪨 Камень",
        },
        new()
        {
            Code = "cringe",
            ExpectedCost = 195,
            NormalizedEmoji = "😬",
            LegacyTitle = "😬 CRINGE",
        },
        new()
        {
            Code = "hello",
            ExpectedCost = 210,
            NormalizedEmoji = "👋",
            LegacyTitle = "👋 Привет",
        },
        new()
        {
            Code = "bye",
            ExpectedCost = 215,
            NormalizedEmoji = "👋",
            LegacyTitle = "👋 Пока",
        },
        new()
        {
            Code = "intelligence",
            ExpectedCost = 317,
            NormalizedEmoji = "🧠",
            LegacyTitle = "🧠 Интеллект",
        },
        new()
        {
            Code = "miku_screamer",
            ExpectedCost = 320,
            NormalizedEmoji = "😱",
            LegacyTitle = "😱 MIKU SCREAMER",
        },
        new()
        {
            Code = "skibidibop",
            ExpectedCost = 333,
            NormalizedEmoji = "🎺",
            LegacyTitle = "🎺 ШКИБИДИБОП",
        },
        new()
        {
            Code = "phonk_edit",
            ExpectedCost = 337,
            NormalizedEmoji = "🎵",
            LegacyTitle = "🎵 Phonk Edit",
        },
        new()
        {
            Code = "aga",
            ExpectedCost = 341,
            NormalizedEmoji = "📽",
            LegacyTitle = "📽️ AGA",
        },
        new()
        {
            Code = "bad_to_bone",
            ExpectedCost = 342,
            NormalizedEmoji = "🎸",
            LegacyTitle = "🎸 BAD TO THE BONE",
        },
        new()
        {
            Code = "tiktok_edit",
            ExpectedCost = 353,
            NormalizedEmoji = "🎬",
            LegacyTitle = "🎬 Make a TikTok Edit",
        },
        new()
        {
            Code = "dance_dance",
            ExpectedCost = 375,
            NormalizedEmoji = "💃",
            LegacyTitle = "💃 DANCE DANCE DANCE",
        },
        new()
        {
            Code = "edge0100_alert",
            ExpectedCost = 666,
            NormalizedEmoji = "👑",
            LegacyTitle = "👑 Специальный алерт для Edge0100",
        },
        new()
        {
            Code = "skibidibop_long",
            ExpectedCost = 1333,
            NormalizedEmoji = "🎺",
            LegacyTitle = "🎺 SKIBIDIBOP LONG",
        },
        new()
        {
            Code = "status_question",
            ExpectedCost = 1510,
            NormalizedEmoji = "❓",
            LegacyTitle = "❓ STATUS?",
        },
        new()
        {
            Code = "miku_miku_beam",
            ExpectedCost = 1580,
            NormalizedEmoji = "✨",
            LegacyTitle = "✨ MIKU MIKU BEAM",
        },
        new()
        {
            Code = "cinema_request",
            ExpectedCost = 1602,
            NormalizedEmoji = "🎬",
            LegacyTitle = "🎬 CINEMA/ANIME REQUEST",
        },
        new()
        {
            Code = "confetti",
            ExpectedCost = 1700,
            NormalizedEmoji = "🎉",
            LegacyTitle = "🎉 Конфетти!",
        },
        new()
        {
            Code = "fireworks",
            ExpectedCost = 1701,
            NormalizedEmoji = "🎆",
            LegacyTitle = "🎆 Феерверк!",
        },
        new()
        {
            Code = "emojis",
            ExpectedCost = 1702,
            NormalizedEmoji = "😀",
            LegacyTitle = "😀 Эмодзи!",
        },
        new()
        {
            Code = "adhd_superpower",
            ExpectedCost = 2002,
            NormalizedEmoji = "⚡",
            LegacyTitle = "⚡ ADHD Superpower",
        },
        new()
        {
            Code = "close_game",
            ExpectedCost = 6666,
            NormalizedEmoji = "💻",
            LegacyTitle = "💻 Выключить игру",
        },
        new()
        {
            Code = "credits",
            ExpectedCost = 8005,
            NormalizedEmoji = "🎬",
            LegacyTitle = "🎬 Credits",
        },
        new()
        {
            Code = "select_game",
            ExpectedCost = 75000,
            NormalizedEmoji = "🎮",
            LegacyTitle = "🎮 Выберите мне игру",
        },
        new()
        {
            Code = "all_refund",
            ExpectedCost = 99999,
            NormalizedEmoji = "💣",
            LegacyTitle = "💣 Алармагеддон 💣",
            RequiresUserInput = true,
        },
    ];

    public static IReadOnlyDictionary<string, RewardDefinition> ByCode { get; } =
        All.ToDictionary(definition => definition.Code, StringComparer.Ordinal);

    public static IReadOnlyDictionary<RewardFingerprint, string> ByFingerprint { get; } =
        All.ToDictionary(definition => definition.Fingerprint, definition => definition.Code);

    public static RewardDefinition Get(string code) => ByCode[code];
}
