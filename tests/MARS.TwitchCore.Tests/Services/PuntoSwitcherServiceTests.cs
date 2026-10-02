using System.Reflection;
using MARS.Shared.Models;
using MARS.TwitchCore.Services.PuntoSwitcher;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// PuntoSwitcher: переключатель раскладки в чате.
///
/// Сервис ловит типичную ошибку — человек набирает русский текст на английской
/// раскладке, — и молча переписывает сообщение. Проверяется, что исправляется
/// именно опечатка раскладки и что обычный текст не трогается: иначе сервис
/// портил бы сообщения живых людей.
/// </summary>
public class PuntoSwitcherServiceTests
{
    /// <summary>
    /// Текст, набранный в английской раскладке вместо русской, приходит без
    /// гласных в неверном алфавите — именно это признак опечатки.
    /// </summary>
    [Fact]
    public void LatinTypingIsConvertedToCyrillic()
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage("ghbdtn");

        Assert.True(result.Success);
        Assert.Equal("привет", result.Result!.CorrectedMessage);
        Assert.Equal(1, result.Result.ReplacedTokens);
        Assert.True(result.Result.HasChanges);
    }

    /// <summary>
    /// Обратный случай: русский текст, набранный в русской раскладке, должен
    /// остаться как есть — иначе сервис ломал бы правильные сообщения.
    /// </summary>
    [Fact]
    public void CorrectCyrillicIsLeftAlone()
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage("привет мир");

        Assert.True(result.Success);
        Assert.Equal("привет мир", result.Result!.CorrectedMessage);
        Assert.Equal(0, result.Result.ReplacedTokens);
        Assert.False(result.Result.HasChanges);
    }

    [Fact]
    public void EveryMistypedTokenIsFixed()
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage("ghbdtn ghbdtn");

        Assert.Equal("привет привет", result.Result!.CorrectedMessage);
        Assert.Equal(2, result.Result.ReplacedTokens);
    }

    /// <summary>
    /// Эмоции Twitch не переводятся: <c>KEKW</c> — это эмоция, а не опечатка,
    /// и переписать его значило бы показать зрителю чужое сообщение.
    /// </summary>
    [Fact]
    public void ProtectedTokensAreNeverTouched()
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage("KEKW Kappa PogChamp");

        Assert.Equal("KEKW Kappa PogChamp", result.Result!.CorrectedMessage);
        Assert.Equal(0, result.Result.ReplacedTokens);
    }

    /// <summary>
    /// Ссылки не трогаются: в них осмысленные смеси латиницы и кириллицы, и
    /// «исправление» сделало бы ссылку битой.
    /// </summary>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://example.org/ghbdtn")]
    public void LinksAreLeftAlone(string message)
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage(message);

        Assert.Equal(message, result.Result!.CorrectedMessage);
        Assert.Equal(0, result.Result.ReplacedTokens);
    }

    /// <summary>
    /// Команда чата не переписывается: иначе <c>!ghbdtn</c> превратился бы в
    /// несуществующую команду.
    /// </summary>
    [Theory]
    [InlineData("!ghbdtn")]
    [InlineData("/ghbdtn")]
    [InlineData(".ghbdtn")]
    public void CommandsAreLeftAlone(string message)
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage(message);

        Assert.Equal(message, result.Result!.CorrectedMessage);
        Assert.Equal(0, result.Result.ReplacedTokens);
    }

    /// <summary>
    /// Ники и теги пропускаются: <c>@ghbdtn</c> — это имя пользователя, а не
    /// опечатка.
    /// </summary>
    [Theory]
    [InlineData("@ghbdtn")]
    [InlineData("#ghbdtn")]
    public void MentionsAreLeftAlone(string token)
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage(token);

        Assert.Equal(token, result.Result!.CorrectedMessage);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("ghbdtn42")]
    [InlineData("ab")]
    public void NonWordsAreLeftAlone(string token)
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage(token);

        Assert.Equal(token, result.Result!.CorrectedMessage);
    }

    /// <summary>
    /// Пунктуация и регистр сохраняются: исправленный текст показывается зрителю,
    /// и «Привет.» должно остаться «Привет.».
    /// </summary>
    [Fact]
    public void PunctuationAndCaseArePreserved()
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage("Ghbdtn, ghbdtn!");

        Assert.Equal("Привет, привет!", result.Result!.CorrectedMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyMessageIsRejected(string? message)
    {
        var service = new PuntoSwitcherService();

        var result = service.TryFixMessage(message);

        Assert.False(result.Success);
    }

    [Fact]
    public void FilterIsEnabledByDefault()
    {
        Assert.True(new PuntoSwitcherService().IsFilterEnabled);
    }

    [Fact]
    public void FilterFlagIsSettable()
    {
        var service = new PuntoSwitcherService { IsFilterEnabled = false };

        Assert.False(service.IsFilterEnabled);
    }

    /// <summary>
    /// Сервис без зависимостей не должен падать на старте: конструктор без
    /// аргументов используется в тестах и при частичной регистрации в DI.
    /// </summary>
    [Fact]
    public async Task StartWithoutDependenciesDoesNotThrow()
    {
        var service = new PuntoSwitcherService();

        await ((Microsoft.Extensions.Hosting.IHostedService)service).StartAsync(
            TestContext.Current.CancellationToken
        );

        await ((Microsoft.Extensions.Hosting.IHostedService)service).StopAsync(
            TestContext.Current.CancellationToken
        );
    }

    /// <summary>
    /// Сообщение TwitchLib неизменяемо, поэтому сервис подменяет поле через
    /// рефлексию. Если поле переименуется, сообщение останется неисправленным
    /// молча — тест фиксирует, что подмена действительно работает.
    /// </summary>
    [Fact]
    public void MessageIsRewrittenThroughBackingField()
    {
        var method = typeof(PuntoSwitcherService).GetMethod(
            "TryOverrideMessage",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        var message = ChatMessage("ghbdtn");
        var rewritten = method!.Invoke(null, [message, "привет"]);

        Assert.Same(message, rewritten);
        Assert.Equal("привет", message.Message);
    }

    [Fact]
    public void BlankCorrectionKeepsOriginalMessage()
    {
        var method = typeof(PuntoSwitcherService).GetMethod(
            "TryOverrideMessage",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        var message = ChatMessage("ghbdtn");
        method!.Invoke(null, [message, "   "]);

        Assert.Equal("ghbdtn", message.Message);
    }

    private static ChatMessage ChatMessage(string message) =>
        new(
            botUsername: "mars-bot",
            userId: "123456789",
            userName: "pyro",
            displayName: "Pyro",
            hexColor: "#FFFFFF",
            emoteSet: null!,
            message: message,
            userType: UserType.Viewer,
            channel: "канал",
            id: "message-1",
            subscribedMonthCount: 0,
            roomId: "room",
            isMe: false,
            isBroadcaster: false,
            noisy: default(Noisy),
            rawIrcMessage: string.Empty,
            emoteReplacedMessage: string.Empty,
            badges: [],
            cheerBadge: null!,
            bits: 0,
            bitsInDollars: 0,
            userDetail: default
        );
}
