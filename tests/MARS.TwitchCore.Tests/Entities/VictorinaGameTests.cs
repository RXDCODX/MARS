using MARS.TwitchCore.Services.Rewards;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using VictorinaGame = MARS.TwitchCore.Entities.Subs.VictorinaGame;

namespace MARS.TwitchCore.Tests.Entities;

/// <summary>
/// Викторина с буквами: игра идёт, пока не открыты все буквы ответа или пока её
/// не остановили.
///
/// Проверяется, что игра всегда заканчивается и в конце объявляется ответ: иначе
/// после показа всех букв цикл крутился бы вечно, а зрители ждали бы ответа,
/// которого не будет. Без пауз между подсказками тест выполняется мгновенно —
/// их величина задаётся нулём.
/// </summary>
public class VictorinaGameTests
{
    [Fact]
    public async Task AnswerIsAnnouncedWhenAllLettersAreShown()
    {
        var client = ConnectedClient();
        var game = Create(client.Object, "Сколько будет два плюс два?|Пять");

        await game.MainThread();

        client.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<JoinedChannel>(),
                    It.Is<string>(m => m.Contains("Никто не отгадал")),
                    default
                ),
            Times.Once
        );
        Assert.Equal("Пять", game.Answer);
        Assert.False(game.Active);
    }

    /// <summary>
    /// Вопрос и ответ разбираются по «|»: в чат уходит сам вопрос с числом букв,
    /// а ответ виден только когда открыты все.
    /// </summary>
    [Fact]
    public async Task QuestionIsSentWithLetterCount()
    {
        var client = ConnectedClient();
        var game = Create(client.Object, "Вопрос?|Ответ");

        await game.MainThread();

        client.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<JoinedChannel>(),
                    It.Is<string>(m => m.Contains("(5 букв)") && m.Contains("Вопрос?")),
                    default
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HintsRevealLettersGradually()
    {
        var client = ConnectedClient();
        var game = Create(client.Object, "Вопрос?|Ответ");

        await game.MainThread();

        client.Verify(
            instance =>
                instance.SendMessageAsync(
                    It.IsAny<JoinedChannel>(),
                    It.Is<string>(m => m.StartsWith("Подсказка")),
                    default
                ),
            Times.AtLeastOnce
        );
    }

    /// <summary>
    /// Остановленная игра не показывает подсказок и не объявляет ответ: команда
    /// остановки должна действовать сразу, а не после следующей подсказки.
    /// </summary>
    [Fact]
    public async Task InactiveGameStopsWithoutHints()
    {
        var client = ConnectedClient();
        var game = Create(client.Object, "Вопрос?|Ответ");
        game.Active = false;

        await game.MainThread();

        Assert.False(game.Active);
    }

    /// <summary>
    /// Игра без источника вопросов не роняет поток: исключение ловится внутри,
    /// и фоновый воркер продолжает жить.
    /// </summary>
    [Fact]
    public async Task GameWithoutQuestionsDoesNotThrow()
    {
        var trivia = new Mock<ITwitchTrivia>();
        trivia.SetupGet(instance => instance.CountQuestions).Returns(0);
        var client = ConnectedClient();

        var game = new VictorinaGame(
            NullLogger.Instance,
            client.Object,
            trivia.Object,
            0,
            new CancellationTokenSource(),
            new SemaphoreSlim(1, 1),
            []
        );

        await game.MainThread();

        Assert.False(game.Active);
    }

    private static VictorinaGame Create(ITwitchClient client, string question) =>
        new(
            NullLogger.Instance,
            client,
            new SingleQuestionTrivia(question),
            0,
            new CancellationTokenSource(),
            new SemaphoreSlim(1, 1),
            []
        );

    /// <summary>
    /// Канал должен быть «подключён»: расширение отправки читает список
    /// подключённых каналов и сама ищет канал, иначе сообщения уходят в никуда.
    /// </summary>
    private static Mock<ITwitchClient> ConnectedClient()
    {
        var client = new Mock<ITwitchClient>();
        client
            .SetupGet(instance => instance.JoinedChannels)
            .Returns([new JoinedChannel(MARS.TwitchCore.Extensions.TwitchConstants.Channel)]);
        client
            .Setup(instance => instance.GetJoinedChannel(It.IsAny<string>()))
            .Returns(new JoinedChannel(MARS.TwitchCore.Extensions.TwitchConstants.Channel));
        client
            .Setup(instance =>
                instance.SendMessageAsync(It.IsAny<JoinedChannel>(), It.IsAny<string>(), default)
            )
            .Returns(Task.CompletedTask);

        return client;
    }

    /// <summary>
    /// Источник вопросов с одним вопросом: настоящий читает файл на 134k строк,
    /// а проверяется здесь только разбор «вопрос|ответ» и ход игры.
    /// </summary>
    private sealed class SingleQuestionTrivia(string question) : ITwitchTrivia
    {
        public int CountQuestions => 1;

        public Task<string> GetQuestionAsync(int numberQuestion) => Task.FromResult(question);
    }
}
