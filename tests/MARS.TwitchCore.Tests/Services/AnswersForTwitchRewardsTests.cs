using MARS.TwitchCore.Services.ChannelRewards;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Ответы на награды канала.
///
/// Ответы видны всем зрителям в чате, поэтому проверяется подстановка имён и
/// произведений на место плейсхолдеров и формат кулдауна.
/// </summary>
public class AnswersForTwitchRewardsTests
{
    [Fact]
    public void UserNameIsSubstituted()
    {
        var answer = AnswersForTwitchRewards.ReplaceKeywordsInAnswer(
            "Pyro",
            AnswersForTwitchRewards.Answers[RewardCommand.GetRandomAnime],
            animeTitle: "Начало"
        );

        Assert.Contains("Pyro", answer);
        Assert.Contains("Начало", answer);
        Assert.DoesNotContain("{", answer);
    }

    [Fact]
    public void WaifuNameIsSubstituted()
    {
        var answer = AnswersForTwitchRewards.ReplaceKeywordsInAnswer(
            "Pyro",
            AnswersForTwitchRewards.Answers[RewardCommand.AddNewWaifu],
            waifu: new WaifuInfo { Name = "Аяка" }
        );

        Assert.Contains("Аяка", answer);
        Assert.DoesNotContain("{", answer);
    }

    /// <summary>
    /// Для произведения берётся аниме, а манга — только если аниме нет: иначе в
    /// ответе зрителя было бы две разные подписи.
    /// </summary>
    [Fact]
    public void AnimeTakesPrecedenceOverManga()
    {
        var answer = AnswersForTwitchRewards.ReplaceKeywordsInAnswer(
            "Pyro",
            AnswersForTwitchRewards.Answers[RewardCommand.RollWaifu],
            waifu: new WaifuInfo
            {
                Name = "Аяка",
                Anime = "Начало",
                Manga = "Сон",
            }
        );

        Assert.Contains("из Начало!", answer);
    }

    [Fact]
    public void MangaIsUsedWhenAnimeIsMissing()
    {
        var answer = AnswersForTwitchRewards.ReplaceKeywordsInAnswer(
            "Pyro",
            AnswersForTwitchRewards.Answers[RewardCommand.RollWaifu],
            waifu: new WaifuInfo { Name = "Аяка", Manga = "Сон" }
        );

        Assert.Contains("из Сон!", answer);
    }

    /// <summary>
    /// Без произведения подставить нечего, и лучше явная ошибка, чем молчаливое
    /// «из  » в чате.
    /// </summary>
    [Fact]
    public void MissingWaifuIsReported()
    {
        Assert.Throws<NullReferenceException>(() =>
            AnswersForTwitchRewards.ReplaceKeywordsInAnswer(
                "Pyro",
                AnswersForTwitchRewards.Answers[RewardCommand.RollWaifu],
                waifu: null
            )
        );
    }

    /// <summary>
    /// Кулдаун печатается целыми минутами: в ответе он виден зрителю.
    /// </summary>
    [Fact]
    public void CooldownIsPrintedInMinutes()
    {
        Assert.Equal("20", AnswersForTwitchRewards.FormatCooldownTime(TimeSpan.FromMinutes(20.5)));
    }

    /// <summary>
    /// Истёкший кулдаун не показывается отрицательным числом: минус в чате выглядел
    /// бы как ошибка.
    /// </summary>
    [Fact]
    public void ExpiredCooldownIsZero()
    {
        Assert.Equal("0", AnswersForTwitchRewards.FormatCooldownTime(TimeSpan.FromMinutes(-5)));
    }

    /// <summary>
    /// У каждой награды есть свой ответ: иначе зритель получил бы пустую строку.
    /// </summary>
    [Fact]
    public void EveryCommandHasAnAnswer()
    {
        foreach (var command in Enum.GetValues<RewardCommand>())
        {
            Assert.True(
                AnswersForTwitchRewards.Answers.ContainsKey(command),
                $"нет ответа для команды {command}"
            );
        }
    }
}
