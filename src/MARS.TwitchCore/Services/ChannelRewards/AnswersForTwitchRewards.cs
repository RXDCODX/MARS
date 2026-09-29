namespace MARS.TwitchCore.Services.ChannelRewards;

public class AnswersForTwitchRewards
{
    public static readonly Dictionary<RewardCommand, string> Answers = new()
    {
        { RewardCommand.AddNewWaifu, "@{user}, новый супруг {waifuName} добавлен(-а)!" },
        { RewardCommand.GetRandomAnime, "@{user}, твое рандомное аниме - {animeTitle}" },
        { RewardCommand.GetRandomManga, "@{user}, твоя рандомная манга - {mangaTitle}" },
        {
            RewardCommand.MergeWaifu,
            "Произошла свадьба между @{user} и {waifuName}! Совет да любовь!"
        },
        { RewardCommand.RollWaifu, "@{user}, тебе выпал(-а) {waifuName} из {waifuTitle}!" },
    };

    private static readonly IEnumerable<string> Keywords =
    [
        "{user}",
        "{waifuName}",
        "{animeTitle}",
        "{mangaTitle}",
        "{waifuTitle}",
    ];

    public static string ReplaceKeywordsInAnswer(
        string displayName,
        string message,
        string? animeTitle = null,
        string? mangaTitle = null,
        WaifuInfo? waifu = null
    )
    {
        if (Keywords.Any(e => message.Contains(e)))
        {
            var keywords = Keywords.Where(e => message.Contains(e));

            foreach (var keyword in keywords)
            {
                switch (keyword)
                {
                    case "{user}":
                        message = message.Replace(keyword, displayName);
                        break;
                    case "{waifuName}":
                        message = message.Replace(
                            keyword,
                            waifu!.Name ?? throw new NullReferenceException("waifu был null # ")
                        );
                        break;
                    case "{animeTitle}":
                        message = message.Replace(
                            keyword,
                            animeTitle
                                ?? throw new NullReferenceException("animeTitle был null")
                        );
                        break;
                    case "{mangaTitle}":
                        message = message.Replace(
                            keyword,
                            mangaTitle
                                ?? throw new NullReferenceException("mangaTitle был null")
                        );
                        break;
                    case "{waifuTitle}":
                        var title = string.IsNullOrWhiteSpace(waifu!.Anime)
                            ? waifu.Manga
                                ?? throw new NullReferenceException(
                                    "waifu.Anime и waifu.Manga был null"
                                )
                            : waifu.Anime;
                        message = message.Replace(keyword, title);
                        break;
                }
            }
        }

        return message;
    }

    public static string FormatCooldownTime(TimeSpan remaining)
    {
        var clamped = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        var totalMinutes = (int)clamped.TotalMinutes;

        return totalMinutes.ToString();
    }
}
