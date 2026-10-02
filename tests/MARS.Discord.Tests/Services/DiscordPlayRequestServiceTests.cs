using System.Globalization;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using MARS.Discord.Models;
using MARS.Discord.Services.Gateway;
using MARS.Discord.Services.PlayRequest;
using MARS.Discord.Services.YouTube;
using MARS.Shared.Models.Media;
using MARS.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Логика команды /play в Discord: разбор запроса, выбор трека из выпадающего
/// списка и тексты, которые видит пользователь.
///
/// Помощники приватные и статические: это чистые функции, и единственный способ
/// дотянуться до них — рефлексия по имени. Ожидания проверяют то, что увидит
/// пользователь, а не то, что вернул внутренний метод.
/// </summary>
public class DiscordPlayRequestServiceTests
{
    [Theory]
    [InlineData("/play трек", true, "трек")]
    [InlineData("!play трек", true, "трек")]
    [InlineData("/PLAY трек", true, "трек")]
    [InlineData("/play", true, "")]
    [InlineData("/play  трек  ", true, "трек")]
    [InlineData("/play2 трек", false, "")]
    [InlineData("просто текст", false, "")]
    [InlineData("", false, "")]
    [InlineData("   ", false, "")]
    public void PlayQueryIsParsedFromMessage(
        string messageText,
        bool expectedFound,
        string expectedQuery
    )
    {
        var method = Private("TryGetPlayQuery");
        object?[] arguments = [messageText, null];

        var found = Invoke<bool>(method, arguments);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expectedQuery, (string)arguments[1]!);
    }

    [Theory]
    [InlineData("discord-play:abc", true, "abc")]
    [InlineData("DISCORD-PLAY:", false, "")]
    [InlineData("другое:abc", false, "")]
    [InlineData("", false, "")]
    public void SessionIdIsReadFromComponentCustomId(
        string customId,
        bool expectedFound,
        string expectedSessionId
    )
    {
        var method = Private("TryGetSessionId");
        object?[] arguments = [customId, null];

        var found = Invoke<bool>(method, arguments);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expectedSessionId, (string)arguments[1]!);
    }

    [Theory]
    [InlineData(new[] { "2" }, 2)]
    [InlineData(new[] { "0" }, 0)]
    [InlineData(new[] { "-1" }, -1)]
    [InlineData(new[] { "не число" }, -1)]
    [InlineData(new string[0], -1)]
    public void SelectedIndexIsParsedFromValues(string[] values, int expected)
    {
        Assert.Equal(expected, Invoke<object?>(Private("ParseSelectedIndex"), [values]));
    }

    /// <summary>
    /// Лимит вложения выбирается по тарифу сервера, но не меньше того, что
    /// сообщил Discord для конкретного взаимодействия: иначе на сервере с
    /// Tier 3 загрузка тихо обрезалась бы.
    /// </summary>
    [Theory]
    [InlineData(DiscordPremiumTier.None, 0L, 10L * 1024 * 1024)]
    [InlineData(DiscordPremiumTier.Tier_1, 0L, 10L * 1024 * 1024)]
    [InlineData(DiscordPremiumTier.Tier_2, 0L, 50L * 1024 * 1024)]
    [InlineData(DiscordPremiumTier.Tier_3, 0L, 100L * 1024 * 1024)]
    [InlineData(DiscordPremiumTier.None, 25L * 1024 * 1024, 25L * 1024 * 1024)]
    [InlineData(DiscordPremiumTier.Tier_3, 25L * 1024 * 1024, 100L * 1024 * 1024)]
    public void AttachmentLimitFollowsHigherOfTierAndInteraction(
        DiscordPremiumTier tier,
        long interactionLimit,
        long expected
    )
    {
        Assert.Equal(
            expected,
            Invoke<long>(Private("ResolveAttachmentLimit"), [interactionLimit, tier])
        );
    }

    [Theory]
    [InlineData("https://open.spotify.com/track/abc", true, "https://open.spotify.com/track/abc")]
    [InlineData("http://example.org/abc", true, "http://example.org/abc")]
    [InlineData("open.spotify.com/track/abc", true, "https://open.spotify.com/track/abc")]
    [InlineData("  https://example.org/abc  ", true, "https://example.org/abc")]
    [InlineData("", false, "")]
    [InlineData("   ", false, "")]
    public void AbsoluteUrlIsRecognized(string query, bool expectedFound, string expectedUrl)
    {
        var method = Private("TryGetAbsoluteUrl");
        object?[] arguments = [query, null];

        var found = Invoke<bool>(method, arguments);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expectedUrl, (string)arguments[1]!);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc", true)]
    [InlineData("https://youtu.be/abc", true)]
    [InlineData("youtube.com/watch?v=abc", true)]
    [InlineData("https://open.spotify.com/track/abc", false)]
    [InlineData("https://example.org/youtube.com", false)]
    [InlineData("", false)]
    public void YouTubeUrlIsRecognizedByHost(string query, bool expected)
    {
        var method = Private("TryGetYouTubeUrl");
        object?[] arguments = [query, null];

        Assert.Equal(expected, Invoke<bool>(method, arguments));
    }

    [Theory]
    [InlineData(0, 0, 0, "??:??")]
    [InlineData(0, 1, 2, "01:02")]
    [InlineData(0, 59, 59, "59:59")]
    [InlineData(1, 0, 0, "01:00:00")]
    [InlineData(2, 30, 5, "02:30:05")]
    public void DurationIsFormattedForDiscord(int hours, int minutes, int seconds, string expected)
    {
        var duration = new TimeSpan(hours, minutes, seconds);

        Assert.Equal(expected, Invoke<object?>(Private("FormatDuration"), [duration]));
    }

    [Theory]
    [InlineData("", 10, "")]
    [InlineData("   ", 10, "")]
    [InlineData("  текст  ", 10, "текст")]
    [InlineData("длинный текст", 8, "длинн...")]
    [InlineData("точнодлинный", 12, "точнодлинный")]
    public void LongTextIsTrimmedWithEllipsis(string text, int maxLength, string expected)
    {
        Assert.Equal(expected, Invoke<object?>(Private("TrimText"), [text, maxLength]));
    }

    [Fact]
    public void EmptySearchResultSaysNothingFound()
    {
        var message = Invoke<string>(Private("BuildSearchResultsMessage"), [Session([])]);

        Assert.Equal("Ничего не найдено.", message);
    }

    [Fact]
    public void SearchResultListsNumberedTracks()
    {
        var message = Invoke<string>(
            Private("BuildSearchResultsMessage"),
            [
                Session([
                    Track("трек-1", TimeSpan.FromMinutes(1)),
                    Track("трек-2", TimeSpan.FromMinutes(2)),
                ]),
            ]
        );

        Assert.Contains("Найдено 2 треков по запросу: запрос", message);
        Assert.Contains("1. автор - трек-1 [01:00]", message);
        Assert.Contains("2. автор - трек-2 [02:00]", message);
        Assert.Contains("10 минут", message);
    }

    [Fact]
    public void SelectedMessageNamesTrackAndDuration()
    {
        var message = Invoke<string>(
            Private("BuildSelectedMessage"),
            [
                Session([Track("трек-1", TimeSpan.FromMinutes(3))]),
                Track("трек-1", TimeSpan.FromMinutes(3)),
            ]
        );

        Assert.Contains("Запрос: запрос", message);
        Assert.Contains("Выбран трек: автор - трек-1 [03:00]", message);
        Assert.Contains("Готовлю аудиофайл", message);
    }

    [Fact]
    public void ExpiredMessageTellsUserToStartOver()
    {
        var message = Invoke<string>(Private("BuildExpiredMessage"), [Session([])]);

        Assert.Contains("запрос", message);
        Assert.Contains("Запусти /play ещё раз", message);
    }

    [Theory]
    [InlineData(true, "автор | 01:00")]
    [InlineData(false, "01:00")]
    public void OptionDescriptionFallsBackToDuration(bool withAuthor, string expected)
    {
        var track = Track("трек", TimeSpan.FromMinutes(1));
        if (!withAuthor)
        {
            track.Authors = null;
        }

        Assert.Equal(expected, Invoke<object?>(Private("BuildOptionDescription"), [track]));
    }

    /// <summary>
    /// Пустой исполнитель не должен давать подпись вида «| 01:00»: в выпадающем
    /// списке это выглядит как обрыв данных.
    /// </summary>
    [Fact]
    public void BlankAuthorIsIgnoredInOptionDescription()
    {
        var track = Track("трек", TimeSpan.FromMinutes(1));
        track.Authors = ["   ", "второй"];

        Assert.Equal("01:00", Invoke<string>(Private("BuildOptionDescription"), [track]));
    }

    [Fact]
    public void DirectTrackMessageNamesVideo()
    {
        var message = Invoke<string>(
            Private("BuildDirectTrackPreparingMessage"),
            [Track("ролик", TimeSpan.FromMinutes(1))]
        );

        Assert.Contains("ролик", message);
        Assert.Contains("готовлю аудиодорожку", message);
    }

    [Fact]
    public void UsageTextExplainsBothModes()
    {
        var text = Invoke<string>(Private("BuildPlayUsageText"), []);

        Assert.Contains("/play <поисковый запрос>", text);
        Assert.Contains("/play <ссылка YouTube>", text);
        Assert.Contains("/play без аргументов", text);
    }

    /// <summary>
    /// Slash-команда сравнивается с зарегистрированной: перерегистрация без
    /// изменений создавала бы вторую команду и ловила 500 в чате.
    /// </summary>
    [Fact]
    public void BuiltPlayCommandMatchesItself()
    {
        var command = BuildPlayCommand();

        Assert.True(IsSamePlayCommand(command));
        Assert.Equal("play", command.Name);
    }

    [Fact]
    public void CommandWithDifferentNameIsNotTheSame()
    {
        var command = new DiscordApplicationCommand("other", "описание");

        Assert.False(IsSamePlayCommand(command));
    }

    [Fact]
    public void CommandWithChangedDescriptionNeedsReregistration()
    {
        var command = new DiscordApplicationCommand(
            "play",
            "старое описание",
            [
                new DiscordApplicationCommandOption(
                    "query",
                    "старый текст",
                    DiscordApplicationCommandOptionType.String,
                    false
                ),
            ]
        );

        Assert.False(IsSamePlayCommand(command));
    }

    [Fact]
    public void CommandWithChangedOptionNeedsReregistration()
    {
        var command = new DiscordApplicationCommand(
            "play",
            "Поиск трека или прямая загрузка по YouTube-ссылке",
            [
                new DiscordApplicationCommandOption(
                    "query",
                    "текст",
                    DiscordApplicationCommandOptionType.Integer,
                    false
                ),
            ]
        );

        Assert.False(IsSamePlayCommand(command));
    }

    [Fact]
    public void CommandWithChangedOptionCountNeedsReregistration()
    {
        var command = new DiscordApplicationCommand(
            "play",
            "Поиск трека или прямая загрузка по YouTube-ссылке",
            [
                new DiscordApplicationCommandOption(
                    "query",
                    "текст",
                    DiscordApplicationCommandOptionType.String,
                    false
                ),
                new DiscordApplicationCommandOption(
                    "extra",
                    "лишний",
                    DiscordApplicationCommandOptionType.String,
                    false
                ),
            ]
        );

        Assert.False(IsSamePlayCommand(command));
    }

    /// <summary>
    /// У команды без параметров <c>Options</c> равен null, а не пустому списку.
    /// Раньше такая команда роняла регистрацию /play на guild, и команда просто
    /// не появлялась в чате.
    /// </summary>
    [Fact]
    public void CommandWithoutOptionsIsNotTheSame()
    {
        var command = new DiscordApplicationCommand(
            "play",
            "Поиск трека или прямая загрузка по YouTube-ссылке"
        );

        Assert.False(IsSamePlayCommand(command));
    }

    /// <summary>
    /// Сессия выбора — то, что переживает между сообщением и выбором трека, и
    /// она обязана протухать: иначе старый dropdown принимал бы выбор час спустя.
    /// </summary>
    [Fact]
    public void SessionExpiresOnlyAfterLifetime()
    {
        var fresh = Session([]);
        var stale = new DiscordPlaySelectionSession
        {
            SessionId = "id",
            ChannelId = 1,
            UserId = 2,
            Query = "запрос",
            Tracks = [],
            CreatedAtUtc = DateTime.Now.AddMinutes(-11),
        };

        Assert.False(fresh.IsExpired(TimeSpan.FromMinutes(10)));
        Assert.True(stale.IsExpired(TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void CreatedSessionKeepsQueryAndTracks()
    {
        var tracks = new[] { Track("трек", TimeSpan.FromMinutes(1)) };
        var session = Invoke<DiscordPlaySelectionSession>(
            Private("CreateSession"),
            [1UL, 2UL, "запрос", tracks]
        );

        Assert.Equal(1UL, session.ChannelId);
        Assert.Equal(2UL, session.UserId);
        Assert.Equal("запрос", session.Query);
        Assert.Contains("трек", session.Tracks[0].Title);
        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
    }

    /// <summary>
    /// Старт подписывается на обработчики ровно один раз: повторная подписка
    /// привела бы к двум ответам на одну команду.
    /// </summary>
    [Fact]
    public async Task HandlersAreRegisteredOnlyOnce()
    {
        var gateway = new Mock<IDiscordGatewayService>();
        gateway
            .Setup(instance => instance.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscordClient?)null);
        var service = Service(gateway.Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);

        gateway.Verify(
            instance =>
                instance.RegisterInteractionCreatedHandler(
                    It.IsAny<
                        Func<DiscordClient, DSharpPlus.EventArgs.InteractionCreatedEventArgs, Task>
                    >()
                ),
            Times.Once
        );
        gateway.Verify(
            instance =>
                instance.RegisterComponentInteractionCreatedHandler(
                    It.IsAny<
                        Func<
                            DiscordClient,
                            DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs,
                            Task
                        >
                    >()
                ),
            Times.Once
        );
        gateway.Verify(
            instance => instance.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task StopClearsSessions()
    {
        var service = Service(Mock.Of<IDiscordGatewayService>());

        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.Empty(SessionIds(service));
    }

    private static IReadOnlyList<string> SessionIds(DiscordPlayRequestService service)
    {
        var field = typeof(DiscordPlayRequestService).GetField(
            "_sessions",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        var sessions = (System.Collections.IDictionary)field!.GetValue(service)!;

        return sessions.Keys.Cast<string>().ToArray();
    }

    /// <summary>
    /// Резолвер YouTube подставляется интерфейсом: сам класс создаёт клиент
    /// YoutubeExplode в конструкторе и уводит тест в сеть.
    /// </summary>
    private static DiscordPlayRequestService Service(IDiscordGatewayService gateway)
    {
        IYouTubeResolver resolver = Mock.Of<IYouTubeResolver>();
        var cache = new DiscordPlayAudioCacheService(
            resolver,
            NullLogger<DiscordPlayAudioCacheService>.Instance
        );

        return new DiscordPlayRequestService(
            gateway,
            resolver,
            cache,
            NullLogger<DiscordPlayRequestService>.Instance
        );
    }

    private static DiscordPlaySelectionSession Session(IReadOnlyList<BaseTrackInfo> tracks) =>
        new()
        {
            SessionId = "сессия",
            ChannelId = 1,
            UserId = 2,
            Query = "запрос",
            Tracks = tracks,
        };

    private static BaseTrackInfo Track(string title, TimeSpan duration) =>
        new()
        {
            TrackName = title,
            Url = new Uri("https://mars.example.org/track"),
            Duration = duration,
            Authors = ["автор"],
        };

    private static DiscordApplicationCommand BuildPlayCommand() =>
        Invoke<DiscordApplicationCommand>(Private("BuildPlayCommand"), []);

    private static bool IsSamePlayCommand(DiscordApplicationCommand command) =>
        Invoke<bool>(Private("IsSamePlayCommand"), [command]);

    private static MethodInfo Private(string name) =>
        typeof(DiscordPlayRequestService).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

    /// <summary>
    /// Массив передаётся как есть: при копировании значения out-параметров вернулись
    /// бы во временный массив и потерялись.
    /// </summary>
    private static T Invoke<T>(MethodInfo method, object?[] arguments) =>
        (T)method.Invoke(null, arguments)!;

    static DiscordPlayRequestServiceTests() =>
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
}
