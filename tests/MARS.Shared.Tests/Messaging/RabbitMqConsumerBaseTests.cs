using System.Reflection;
using System.Text;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MARS.Shared.Tests.Messaging;

/// <summary>
/// Потребитель RabbitMQ: подтверждение при успехе, повтор с ростом счётчика при
/// ошибке и dead-letter-очередь после исчерпания попыток.
///
/// Обработка доставки приватная, а канал берётся из приватного поля, поэтому
/// проверка идёт рефлексией. Альтернативы нет: подсунуть настоящий брокер —
/// значит поднять docker, а конструктор потребителя всё равно создаёт фабрику
/// подключения, то есть тест проверял бы сеть, а не обработку сообщения.
/// </summary>
public sealed class RabbitMqConsumerBaseTests
{
    private const string QueueName = "mars.test.queue";
    private const string RoutingKey = "test.event";

    [Fact]
    public async Task SuccessfulMessageIsAcknowledged()
    {
        var channel = new Mock<IChannel>(MockBehavior.Loose);
        var consumer = CreateConsumer(null);
        SetChannel(consumer, channel.Object);

        await DeliverAsync(consumer, retryHeaders: null);

        channel.Verify(
            instance =>
                instance.BasicAckAsync(It.IsAny<ulong>(), false, It.IsAny<CancellationToken>()),
            Times.Once
        );
        channel.Verify(
            instance =>
                instance.BasicNackAsync(
                    It.IsAny<ulong>(),
                    false,
                    false,
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task HandlerReceivesDecodedJsonAndRoutingKey()
    {
        string? receivedJson = null;
        string? receivedKey = null;
        var channel = new Mock<IChannel>(MockBehavior.Loose);
        var consumer = CreateConsumer(
            (key, json, _) =>
            {
                receivedKey = key;
                receivedJson = json;
                return Task.CompletedTask;
            }
        );
        SetChannel(consumer, channel.Object);

        await DeliverAsync(consumer, retryHeaders: null, body: "{\"id\":7}");

        Assert.Equal(RoutingKey, receivedKey);
        Assert.Equal("{\"id\":7}", receivedJson);
    }

    [Fact]
    public async Task FailureIsRepublishedWithIncrementedRetryCount()
    {
        var published = new List<PublishedMessage>();
        var channel = RecordingChannel(published);
        var consumer = CreateConsumer((_, _, _) => throw new InvalidOperationException("сбой"));
        SetChannel(consumer, channel.Object);

        await DeliverAsync(consumer, retryHeaders: null);

        var message = Assert.Single(published);
        Assert.Equal(RabbitMqConfig.ExchangeName, message.Exchange);
        Assert.Equal(RoutingKey, message.RoutingKey);
        Assert.Equal(1, message.Properties!.Headers![RabbitMqConsumerBase.RetryHeader]);
        channel.Verify(
            instance =>
                instance.BasicNackAsync(
                    It.IsAny<ulong>(),
                    false,
                    false,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Сообщение с уже проставленным счётчиком считается следующей попыткой:
    /// без чтения заголовка потребитель либо повторял бы до бесконечности,
    /// либо уводил в dead-letter с первой попытки.
    /// </summary>
    [Fact]
    public async Task ExhaustedMessageGoesToDeadLetterWithReason()
    {
        var published = new List<PublishedMessage>();
        var channel = RecordingChannel(published);
        var consumer = CreateConsumer(
            (_, _, _) => throw new InvalidOperationException("сбой"),
            maxDeliveryAttempts: 2
        );
        SetChannel(consumer, channel.Object);

        await DeliverAsync(
            consumer,
            retryHeaders: new Dictionary<string, object?> { [RabbitMqConsumerBase.RetryHeader] = 1 }
        );

        var message = Assert.Single(published);
        Assert.Equal(string.Empty, message.Exchange);
        Assert.Equal($"{QueueName}.dlq", message.RoutingKey);
        Assert.False(message.Properties!.Headers!.ContainsKey(RabbitMqConsumerBase.RetryHeader));
        Assert.Equal(1, message.Properties.Headers["mars.dead_letter.reason"]);
    }

    [Fact]
    public async Task DeadLetterReasonIsUnknownWithoutRetryHeader()
    {
        var published = new List<PublishedMessage>();
        var channel = RecordingChannel(published);
        var consumer = CreateConsumer(
            (_, _, _) => throw new InvalidOperationException("сбой"),
            maxDeliveryAttempts: 1
        );
        SetChannel(consumer, channel.Object);

        await DeliverAsync(consumer, retryHeaders: null);

        Assert.Equal(
            "unknown",
            Assert.Single(published).Properties!.Headers!["mars.dead_letter.reason"]
        );
    }

    [Fact]
    public async Task DeliveryWithoutChannelIsIgnored()
    {
        var consumer = CreateConsumer(null);

        await DeliveryHandler(consumer)(
            new object(),
            new BasicDeliverEventArgs(
                "tag",
                1,
                false,
                "ex",
                RoutingKey,
                new BasicProperties(),
                Encoding.UTF8.GetBytes("{}")
            )
        );
    }

    /// <summary>
    /// Счётчик попыток приходит из разных клиентов RabbitMQ: int, long, byte
    /// или строка. Неизвестный формат читается как ноль, а не роняет обработку
    /// сообщения.
    /// </summary>
    [Theory]
    [InlineData(3, 4)]
    [InlineData(4L, 5)]
    [InlineData((byte)2, 3)]
    [InlineData("6", 7)]
    [InlineData(null, 1)]
    public async Task RetryCountIsReadFromEveryHeaderRepresentation(
        object? headerValue,
        int expectedAttempt
    )
    {
        var published = new List<PublishedMessage>();
        var channel = RecordingChannel(published);
        var consumer = CreateConsumer(
            (_, _, _) => throw new InvalidOperationException("сбой"),
            maxDeliveryAttempts: 10
        );
        SetChannel(consumer, channel.Object);
        var headers = headerValue is null
            ? null
            : new Dictionary<string, object?> { [RabbitMqConsumerBase.RetryHeader] = headerValue };

        await DeliverAsync(consumer, headers);

        Assert.Equal(
            expectedAttempt,
            Assert.Single(published).Properties!.Headers![RabbitMqConsumerBase.RetryHeader]
        );
    }

    [Fact]
    public async Task StopClosesChannel()
    {
        var channel = new Mock<IChannel>(MockBehavior.Loose);
        var consumer = CreateConsumer(null);
        SetChannel(consumer, channel.Object);

        await consumer.StopAsync(TestContext.Current.CancellationToken);
        await consumer.DisposeAsync();

        channel.Verify(instance => instance.DisposeAsync(), Times.AtLeastOnce);
    }

    /// <summary>
    /// Разбор JSON в потребителе нечувствителен к регистру: сообщения приходят
    /// и от .NET-сериализатора, и от других потребителей RabbitMQ.
    /// </summary>
    [Fact]
    public void ProtectedDeserializerIgnoresPropertyCase()
    {
        var value = DeserializeThroughConsumer();

        Assert.Equal(7, Assert.IsType<DeserializationProbe>(value).Id);
    }

    private static object? DeserializeThroughConsumer()
    {
        var method = typeof(RabbitMqConsumerBase)
            .GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(DeserializationProbe));

        return method.Invoke(null, ["{ \"id\": 7 }"]);
    }

    private static TestConsumer CreateConsumer(
        Func<string, string, CancellationToken, Task>? handler,
        int maxDeliveryAttempts = 3
    ) =>
        new(
            new RabbitMqOptions
            {
                Host = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                MaxDeliveryAttempts = maxDeliveryAttempts,
                ReconnectDelayMilliseconds = 500,
            },
            handler,
            NullLogger.Instance
        );

    private static Mock<IChannel> RecordingChannel(List<PublishedMessage> published)
    {
        var channel = new Mock<IChannel>(MockBehavior.Loose);
        channel
            .Setup(instance =>
                instance.BasicPublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<
                string,
                string,
                bool,
                BasicProperties,
                ReadOnlyMemory<byte>,
                CancellationToken
            >(
                (exchange, routingKey, _, properties, _, _) =>
                    published.Add(new PublishedMessage(exchange, routingKey, properties))
            )
            .Returns(new ValueTask());

        return channel;
    }

    private static void SetChannel(RabbitMqConsumerBase consumer, IChannel channel)
    {
        typeof(RabbitMqConsumerBase)
            .GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(consumer, channel);
    }

    private static Func<object, BasicDeliverEventArgs, Task> DeliveryHandler(
        RabbitMqConsumerBase consumer
    ) =>
        (Func<object, BasicDeliverEventArgs, Task>)
            typeof(RabbitMqConsumerBase)
                .GetMethod("HandleDeliveryAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .CreateDelegate(typeof(Func<object, BasicDeliverEventArgs, Task>), consumer);

    private static Task DeliverAsync(
        RabbitMqConsumerBase consumer,
        Dictionary<string, object?>? retryHeaders,
        string body = "{}"
    )
    {
        var args = new BasicDeliverEventArgs(
            "tag",
            1,
            false,
            "ex",
            RoutingKey,
            new BasicProperties { Headers = retryHeaders },
            Encoding.UTF8.GetBytes(body)
        );

        return DeliveryHandler(consumer)(new object(), args);
    }

    /// <summary>
    /// Закрытый канал заставляет потребителя переподключаться, иначе сервис молча
    /// перестал бы получать события после обрыва брокера.
    /// </summary>
    [Fact]
    public async Task ClosedChannelIsReportedAsFailure()
    {
        var consumer = CreateConsumer(null);
        SetChannel(consumer, Mock.Of<IChannel>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeAsync(consumer, "WaitUntilChannelClosesAsync", [Token])
        );
    }

    /// <summary>
    /// Отмена токена завершает ожидание закрытия канала: иначе остановка сервиса
    /// ждала бы секундный цикл до конца.
    /// </summary>
    [Fact]
    public async Task CancellationStopsWaitingForChannel()
    {
        var consumer = CreateConsumer(null);
        var channel = new Mock<IChannel>();
        channel.SetupGet(instance => instance.IsOpen).Returns(true);
        SetChannel(consumer, channel.Object);
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeAsync(consumer, "WaitUntilChannelClosesAsync", [stopping.Token])
        );
    }

    /// <summary>
    /// Фоновый цикл потребителя возвращается по отмене, а не крутится вечно: иначе
    /// остановка сервиса не завершала бы задачу.
    /// </summary>
    [Fact]
    public async Task BackgroundLoopReturnsOnCancellation()
    {
        var consumer = CreateConsumer(null);
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        var execute = typeof(RabbitMqConsumerBase).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)execute.Invoke(consumer, [stopping.Token])!;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Task InvokeAsync(RabbitMqConsumerBase consumer, string name, object?[] arguments)
    {
        var method = typeof(RabbitMqConsumerBase).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(consumer, arguments)!;
    }

    private static BasicDeliverEventArgs DeliveryArgs() =>
        new(
            "tag",
            1,
            false,
            "ex",
            RoutingKey,
            new BasicProperties { Headers = null },
            Encoding.UTF8.GetBytes("{}")
        );

    private sealed record PublishedMessage(
        string Exchange,
        string RoutingKey,
        BasicProperties? Properties
    );

    private sealed class DeserializationProbe
    {
        public int Id { get; set; }
    }

    private sealed class TestConsumer(
        RabbitMqOptions options,
        Func<string, string, CancellationToken, Task>? handler,
        ILogger logger
    ) : RabbitMqConsumerBase(options, "test-service", QueueName, [RoutingKey], logger)
    {
        protected override Task HandleMessageAsync(
            string routingKey,
            string json,
            CancellationToken ct
        ) => handler is null ? Task.CompletedTask : handler(routingKey, json, ct);
    }
}
