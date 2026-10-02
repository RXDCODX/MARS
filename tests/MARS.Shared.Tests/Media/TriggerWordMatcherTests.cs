using MARS.Shared.Media;
using MARS.Shared.Models.Media;

namespace MARS.Shared.Tests.Media;

/// <summary>
/// Подбор алерта по ключевому слову из сообщения чата.
/// Правила перенесены из <c>TwitchMessagesHubAwaker</c> монолита.
/// </summary>
public class TriggerWordMatcherTests
{
    private static MediaInfo Alert(string? triggerWord, bool isEnabled = true) =>
        new()
        {
            TextInfo = new MediaTextInfo { TriggerWord = triggerWord },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Video,
                FilePath = "alert.mp4",
                FileName = "alert.mp4",
                Extension = ".mp4",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "alert", IsEnabled = isEnabled },
            StylesInfo = new MediaStylesInfo(),
        };

    [Fact]
    public void Match_FindsTheAlertByAPlainWord()
    {
        var alerts = new[] { Alert("привет") };

        var result = TriggerWordMatcher.Match(alerts, "всем привет в чате");

        Assert.Single(result);
    }

    /// <summary>
    /// Регистр не важен: триггер пишут от руки, а сообщения пишут как попало.
    /// </summary>
    [Fact]
    public void Match_IgnoresCase()
    {
        var alerts = new[] { Alert("Привет") };

        var result = TriggerWordMatcher.Match(alerts, "ПРИВЕТ всем");

        Assert.Single(result);
    }

    /// <summary>
    /// Границы слова обязательны: триггер «привет» не должен срабатывать на
    /// «приветствие», иначе один короткий триггер поднимал бы лишние алерты.
    /// </summary>
    [Fact]
    public void Match_RequiresWordBoundaries()
    {
        var alerts = new[] { Alert("привет") };

        var result = TriggerWordMatcher.Match(alerts, "прощайте");

        Assert.Empty(result);
    }

    [Fact]
    public void Match_FindsAPhraseWithSpaces()
    {
        var alerts = new[] { Alert("добрый вечер") };

        var result = TriggerWordMatcher.Match(alerts, " всем добрый вечер ");

        Assert.Single(result);
    }

    [Fact]
    public void Match_SupportsRegularExpressions()
    {
        var alerts = new[] { Alert(@"прив\w+") };

        var result = TriggerWordMatcher.Match(alerts, "приветики всем");

        Assert.Single(result);
    }

    [Fact]
    public void Match_SkipsDisabledAlerts()
    {
        var alerts = new[] { Alert("привет", isEnabled: false) };

        var result = TriggerWordMatcher.Match(alerts, "привет");

        Assert.Empty(result);
    }

    [Fact]
    public void Match_SkipsAlertsWithoutTriggerWord()
    {
        var alerts = new[] { Alert(null), Alert("   ") };

        var result = TriggerWordMatcher.Match(alerts, "привет");

        Assert.Empty(result);
    }

    [Fact]
    public void Match_ReturnsNothing_ForAnEmptyMessage()
    {
        var alerts = new[] { Alert("привет") };

        Assert.Empty(TriggerWordMatcher.Match(alerts, "   "));
    }

    [Fact]
    public void Match_ReportsEveryMatchingAlert()
    {
        var alerts = new[] { Alert("привет"), Alert("пока") };

        var result = TriggerWordMatcher.Match(alerts, "привет и пока");

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Регулярные выражения собираются с NonBacktracking: текст из чата не
    /// должен быть поводом для ReDoS.
    /// </summary>
    [Fact]
    public void Match_SurvivesACatastrophicPattern()
    {
        var alerts = new[] { Alert(@"(a+)+b") };

        var result = TriggerWordMatcher.Match(alerts, new string('a', 40) + "c");

        Assert.Empty(result);
    }

    /// <summary>
    /// Фраза в кавычках — один токен: <c>SplitWithQuotes</c> из монолита держит
    /// её целиком, иначе <c>"добрый вечер"</c> распался бы на два слова и
    /// поднимал бы алерт на «всем добрым вечером».
    /// </summary>
    [Fact]
    public void Match_TreatsAQuotedPhraseAsOneTrigger()
    {
        var alerts = new[] { Alert("\"добрый вечер\"") };

        Assert.Single(TriggerWordMatcher.Match(alerts, "всем добрый вечер"));
        Assert.Empty(TriggerWordMatcher.Match(alerts, "добрый вечером"));
    }

    /// <summary>
    /// Незакрытая кавычка в монолите роняла весь проход по алертам. Здесь такой
    /// триггер просто не срабатывает: чужая ошибка в настройке не должна
    /// глушить остальные алерты.
    /// </summary>
    [Fact]
    public void Match_SkipsTriggerWithUnbalancedQuotes()
    {
        var alerts = new[] { Alert("\"привет") };

        Assert.Empty(TriggerWordMatcher.Match(alerts, "привет"));
    }

    /// <summary>
    /// Токен, который не компилируется как регулярное выражение, идёт путём
    /// обычного сравнения — так же, как в монолите, где регуляркой считалось
    /// только то, что <c>new Regex</c> смог собрать.
    /// </summary>
    [Fact]
    public void Match_TreatsAnUncompilableTokenAsAPlainWord()
    {
        var alerts = new[] { Alert("*звёзды*") };

        Assert.Single(TriggerWordMatcher.Match(alerts, "люблю *звёзды* сегодня"));
        Assert.Empty(TriggerWordMatcher.Match(alerts, "звёзды"));
    }

    /// <summary>
    /// NonBacktracking не умеет ретроспективные проверки. Монолит на таком
    /// триггере падал, здесь он молча не совпадает — сообщение из чата не
    /// должно ронять обработку.
    /// </summary>
    [Fact]
    public void Match_SkipsPatternUnsupportedByNonBacktracking()
    {
        var alerts = new[] { Alert("(?<=x)привет") };

        Assert.Empty(TriggerWordMatcher.Match(alerts, "привет"));
    }

    /// <summary>
    /// Якоря в триггере сохраняются: <c>^привет</c> должен ловить начало
    /// сообщения, а не середину.
    /// </summary>
    [Fact]
    public void Match_KeepsExplicitAnchors()
    {
        var alerts = new[] { Alert("^привет") };

        Assert.Single(TriggerWordMatcher.Match(alerts, "привет всем"));
        Assert.Empty(TriggerWordMatcher.Match(alerts, "всем привет"));
    }

    /// <summary>
    /// Алерт с двумя сработавшими триггерами возвращается один раз: монолит
    /// добавлял его в список дважды и он становился вдвое вероятнее в розыгрыше.
    /// </summary>
    [Fact]
    public void Match_ReportsAnAlertOnce_EvenWithTwoMatchingTriggers()
    {
        var alerts = new[] { Alert("привет") };

        Assert.Single(TriggerWordMatcher.Match(alerts, "привет, привет"));
    }
}
