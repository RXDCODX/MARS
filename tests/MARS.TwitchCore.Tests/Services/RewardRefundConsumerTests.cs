using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.Rewards;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Возврат баллов за отклонённый редемпшен.
/// </summary>
/// <remarks>
/// <para>
/// Успешный путь проверяется только до вызова Twitch API: <c>Helix</c> — конкретный
/// класс, а тела ответов в нём не подделываются (см. заметку в AGENTS.md). Поэтому
/// набор держит решения потребителя, которые принимаются до сети: разобралось ли
/// сообщение, хватает ли идентификаторов и есть ли токен стримера.
/// </para>
/// <para>
/// Отказ не бросает исключение: сообщение без идентификаторов или без токена — это
/// не яд, ретраить его бессмысленно. Бросок здесь увёл бы сообщение в DLQ, а баллы
/// зрителю всё равно бы не вернулись.
/// </para>
/// </remarks>
public class RewardRefundConsumerTests
{
    /// <summary>
    /// Тело <c>null</c> разбирается в <c>null</c> и пропускается тихо: это не
    /// битый JSON, а пустое сообщение.
    /// </summary>
    [Fact]
    public async Task NullBodyIsSkipped()
    {
        var logger = new RecordingLogger<RewardRefundConsumer>();

        await HandleAsync("null", logger: logger);

        Assert.Contains(logger.Messages, message => message.Contains("could not be deserialized"));
    }

    /// <summary>
    /// Без идентификатора награды или редемпшена отменять нечего: в журнал уходит
    /// предупреждение, в Twitch — ничего.
    /// </summary>
    [Theory]
    [InlineData("", "redemption-1")]
    [InlineData("   ", "redemption-1")]
    [InlineData("reward-1", "")]
    [InlineData("reward-1", "   ")]
    public async Task MissingIdentifierIsSkipped(string rewardId, string redemptionId)
    {
        var logger = new RecordingLogger<RewardRefundConsumer>();

        await HandleAsync(Event(rewardId, redemptionId), logger: logger);

        Assert.Contains(logger.Messages, message => message.Contains("no reward or redemption id"));
    }

    /// <summary>
    /// Второй клиент Twitch API не заводится: возврат исполняет владелец токена.
    /// Без токена потребитель обязан промолчать, а не падать.
    /// </summary>
    [Fact]
    public async Task MissingTokenIsSkipped()
    {
        var logger = new RecordingLogger<RewardRefundConsumer>();

        await HandleAsync(Event(), logger: logger);

        Assert.Contains(logger.Messages, message => message.Contains("Twitch token is missing"));
    }

    /// <summary>
    /// Свежий токен есть — потребитель доходит до Twitch API. У подставного
    /// <see cref="ITwitchAPI"/> свойство <c>Helix</c> пустое, и обращение к нему
    /// падает <see cref="NullReferenceException"/>: именно это и доказывает, что
    /// отказ «нет токена» не сработал. Ответ самого Twitch здесь не проверяется —
    /// его нельзя подделать, не проверив вместо этого сеть.
    /// </summary>
    [Fact]
    public async Task FreshTokenReachesTwitchApi()
    {
        var tokenService = await TwitchTokenServiceStub.WithTokenAsync(
            cancellationToken: TestContext.Current.CancellationToken
        );
        var logger = new RecordingLogger<RewardRefundConsumer>();

        var exception = await Record.ExceptionAsync(() =>
            HandleAsync(Event(), tokenService, logger: logger)
        );

        Assert.IsType<NullReferenceException>(exception);
        Assert.DoesNotContain(
            logger.Messages,
            message => message.Contains("Twitch token is missing")
        );
    }

    private static Task HandleAsync(
        string json,
        TokenService? tokenService = null,
        ITwitchAPI? api = null,
        RecordingLogger<RewardRefundConsumer>? logger = null
    )
    {
        var consumer = new RewardRefundConsumer(
            Options.Create(new RabbitMqOptions()),
            api ?? Mock.Of<ITwitchAPI>(),
            tokenService ?? TwitchTokenServiceStub.WithoutToken(),
            logger ?? new RecordingLogger<RewardRefundConsumer>()
        );
        var method = typeof(RewardRefundConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)
            method.Invoke(
                consumer,
                [
                    RabbitMqConfig.RoutingKeys.RewardRefund,
                    json,
                    TestContext.Current.CancellationToken,
                ]
            )!;
    }

    private static string Event(
        string rewardId = "reward-1",
        string redemptionId = "redemption-1"
    ) =>
        JsonSerializer.Serialize(
            new RewardRefundEvent
            {
                RewardId = rewardId,
                RedemptionId = redemptionId,
                UserName = "Pyro",
                Reason = "повторная активация в этом стриме",
            }
        );

    /// <summary>
    /// Собирает сообщения журнала: проверяется не факт записи, а какое именно
    /// решение принял потребитель.
    /// </summary>
    /// <remarks>
    /// Свой логгер, а не Moq: <c>Callback</c> с <c>It.IsAnyType</c> в Moq не
    /// компилируется, а ради трёх проверок тянуть бандлер тестов не нужно.
    /// </remarks>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => _messages.Enqueue(formatter(state, exception));
    }
}
