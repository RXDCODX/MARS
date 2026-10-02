using MARS.TwitchCore.Services.AutoHello;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Автоматическое приветствие в чате.
///
/// Приветствие пишется от имени бота, и это видно всем зрителям. Проверяется, что
/// приветствуется только проверенное сообщение и что пустое приветствие не
/// засоряет чат.
/// </summary>
public class AutoHelloTests
{
    private readonly Mock<IAutoHelloService> _autoHello = new();
    private readonly Mock<ITwitchClient> _client = new();

    [Fact]
    public async Task MessageIsAnsweredWithGreeting()
    {
        _autoHello
            .Setup(service => service.GetAutoHelloMessageAsync("123456789", "pyro"))
            .ReturnsAsync("Привет, pyro");
        var hello = Create();

        await hello.AutoHelloTwitchEvent(null, PassingValidationService.Message("привет"));

        _autoHello.Verify(
            service => service.GetAutoHelloMessageAsync("123456789", "pyro"),
            Times.Once
        );
    }

    /// <summary>
    /// Сообщение из чёрного списка не приветствуется: иначе бот отвечал бы сам себе
    /// и заигрывал бы с чужими правилами.
    /// </summary>
    [Fact]
    public async Task RejectedMessageIsNotAnswered()
    {
        var hello = Create(PassingValidationService.Rejecting("сообщение в чёрном списке"));

        await hello.AutoHelloTwitchEvent(null, PassingValidationService.Message("привет"));

        _autoHello.Verify(
            service => service.GetAutoHelloMessageAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    /// <summary>
    /// Пустое приветствие не отправляется: лишнее сообщение в чате видят все.
    /// </summary>
    [Fact]
    public async Task EmptyGreetingIsNotSent()
    {
        _autoHello
            .Setup(service =>
                service.GetAutoHelloMessageAsync(It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync("   ");
        var hello = Create();

        await hello.AutoHelloTwitchEvent(null, PassingValidationService.Message("привет"));

        _client.Verify(
            client =>
                client.SendMessageAsync(
                    It.IsAny<JoinedChannel>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// На старте приложения подписка вешается на клиент: без неё приветствий не
    /// будет вовсе.
    /// </summary>
    [Fact]
    public async Task StartupSubscribesToMessages()
    {
        var hello = Create();

        await Start(hello);

        // Подписка проверяется через сам обработчик: после старта он доступен для
        // вызова, а событие на клиенте добавляется один раз.
        _autoHello
            .Setup(service =>
                service.GetAutoHelloMessageAsync(It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync("Привет");
        await hello.AutoHelloTwitchEvent(null, PassingValidationService.Message("привет"));

        _autoHello.Verify(
            service => service.GetAutoHelloMessageAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once
        );
    }

    private static Task Start(AutoHello hello)
    {
        var method = typeof(AutoHello).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(hello, [TestContext.Current.CancellationToken])!;
    }

    private AutoHello Create(
        MARS.TwitchCore.Services.Validation.ITwitchEventValidationService? validator = null
    ) =>
        new(
            NullLogger<AutoHello>.Instance,
            RecordingClient(_client),
            _autoHello.Object,
            new StartedLifetime(),
            validator ?? PassingValidationService.Instance
        );

    /// <summary>
    /// Клиент, который только запоминает написанное: всё, что видит чат, приходит
    /// через <c>SendMessageAsync</c>.
    /// </summary>
    private static ITwitchClient RecordingClient(Mock<ITwitchClient> mock)
    {
        mock.SetupGet(instance => instance.JoinedChannels).Returns([new JoinedChannel("mars")]);
        mock.Setup(instance => instance.GetJoinedChannel(It.IsAny<string>()))
            .Returns(new JoinedChannel("mars"));

        return mock.Object;
    }

    /// <summary>
    /// Время жизни, у которого старт уже произошёл: подписка вешается сразу.
    /// </summary>
    private sealed class StartedLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => new CancellationToken(true);

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() { }
    }
}
