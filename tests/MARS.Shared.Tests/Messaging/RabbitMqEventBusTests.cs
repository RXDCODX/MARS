using System.Reflection;
using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MARS.Shared.Tests.Messaging;

/// <summary>
/// Продюсер событий RabbitMQ.
///
/// Продюсер лениво поднимает соединение и переподключается после обрыва: если бы
/// он падал на первой же публикации, сервис молча перестал бы отправлять события
/// до перезапуска. Брокера в тесте нет — соединение и канал подставляются
/// заглушками, и проверяется именно логика продюсера.
/// </summary>
public class RabbitMqEventBusTests
{
    /// <summary>
    /// Событие уходит в обмен с ключом маршрутизации, метками сервиса,
    /// MessageId и CorrelationId: по ним потребитель понимает, от кого пришло
    /// событие, и может дедублицировать повторную доставку.
    /// </summary>
    [Fact]
    public async Task EventIsPublishedWithServiceHeaders()
    {
        var channel = OpenChannel();
        var bus = CreateBus(channel.Object);

        var publishTask = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "привет" },
            TestContext.Current.CancellationToken
        );

        // Даём PublishCoreAsync время создать confirmTcs, затем симулируем подтверждение брокера.
        await Task.Yield();
        channel.Raise(c => c.BasicAcksAsync += null, new object(), new BasicAckEventArgs(1, false));

        await publishTask;

        var published = Assert.Single(
            channel.Invocations,
            call => call.Method.Name == "BasicPublishAsync"
        );
        var arguments = published.Arguments!;
        var properties = arguments.OfType<BasicProperties>().Single();

        Assert.Equal(RabbitMqConfig.ExchangeName, arguments.OfType<string>().First());
        Assert.Equal(RabbitMqConfig.ChatSend, arguments.OfType<string>().Skip(1).First());
        Assert.True(properties.Persistent);
        Assert.Equal("application/json", properties.ContentType);
        Assert.Equal("publisher", properties.Headers?["mars.source_service"]);
        Assert.False(string.IsNullOrEmpty(properties.MessageId));
        Assert.Equal(properties.MessageId, properties.CorrelationId);
        Assert.True(arguments.OfType<bool>().First()); // mandatory = true
    }

    /// <summary>
    /// Закрытый брокером канал восстанавливается и публикация повторяется: иначе
    /// после обрыва события уходили бы в никуда.
    /// </summary>
    [Fact]
    public async Task ClosedChannelIsRecreatedAndPublishRetried()
    {
        var channel = OpenChannel();
        var attempts = 0;
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
            .Returns(() =>
            {
                attempts++;

                if (attempts == 1)
                {
                    throw new AlreadyClosedException(
                        new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "channel closed")
                    );
                }

                return ValueTask.CompletedTask;
            });
        var bus = CreateBus(channel.Object);

        var publishTask = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "привет" },
            TestContext.Current.CancellationToken
        );

        // Даём PublishCoreAsync время создать confirmTcs для первого вызова,
        // затем симулируем подтверждения брокера.
        await Task.Yield();
        channel.Raise(c => c.BasicAcksAsync += null, new object(), new BasicAckEventArgs(1, false));
        channel.Raise(c => c.BasicAcksAsync += null, new object(), new BasicAckEventArgs(2, false));

        await publishTask;

        Assert.Equal(2, attempts);
    }

    /// <summary>
    /// Unroutable-сообщение (mandatory: true без подходящей очереди)
    /// обнаруживается через BasicReturnAsync: публикация не считается успешной,
    /// метрика Published не увеличивается, а вызывающая сторона получает исключение.
    /// </summary>
    [Fact]
    public async Task UnroutableMessageThrowsAndIsNotCountedAsPublished()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(instance => instance.IsOpen).Returns(true);
        channel
            .Setup(instance =>
                instance.GetNextPublishSequenceNumberAsync(It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(1UL);

        BasicProperties? publishedProperties = null;
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
            >((_, _, _, properties, _, _) => publishedProperties = properties)
            .Returns(ValueTask.CompletedTask);

        var bus = CreateBus(channel.Object);

        var publishTask = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "привет" },
            TestContext.Current.CancellationToken
        );

        await Task.Yield();
        channel.Raise(
            c => c.BasicReturnAsync += null,
            new object(),
            new BasicReturnEventArgs(
                1,
                "NO_ROUTE",
                RabbitMqConfig.ExchangeName,
                RabbitMqConfig.ChatSend,
                new BasicProperties { MessageId = publishedProperties!.MessageId },
                ReadOnlyMemory<byte>.Empty,
                CancellationToken.None
            )
        );
        channel.Raise(c => c.BasicAcksAsync += null, new object(), new BasicAckEventArgs(1, false));

        await Assert.ThrowsAsync<InvalidOperationException>(() => publishTask);
    }

    /// <summary>
    /// При неопределённой ошибке BasicPublishAsync канал сбрасывается:
    /// поздний ACK старой публикации не сможет подтвердить следующую.
    /// </summary>
    [Fact]
    public async Task IndeterminatePublishFailureDisposesChannel()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(instance => instance.IsOpen).Returns(true);
        ulong sequence = 0;
        channel
            .Setup(instance =>
                instance.GetNextPublishSequenceNumberAsync(It.IsAny<CancellationToken>())
            )
            .Returns(() => ValueTask.FromResult(++sequence));
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
            .Throws(new InvalidOperationException("publish failed"));

        var bus = CreateBus(channel.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bus.PublishAsync(
                RabbitMqConfig.ChatSend,
                new { Text = "first" },
                TestContext.Current.CancellationToken
            )
        );

        channel.Verify(instance => instance.DisposeAsync(), Times.Once);
    }

    /// <summary>
    /// Return сам по себе не завершает confirm; ACK должен завершить тот же sequence.
    /// После Return → ACK следующая публикация подтверждается только собственным ACK.
    /// </summary>
    [Fact]
    public async Task ReturnThenAckDoesNotCompleteTheNextPublish()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(instance => instance.IsOpen).Returns(true);

        ulong sequence = 0;
        var properties = new List<BasicProperties>();
        channel
            .Setup(instance =>
                instance.GetNextPublishSequenceNumberAsync(It.IsAny<CancellationToken>())
            )
            .Returns(() => ValueTask.FromResult(++sequence));
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
            >((_, _, _, basicProperties, _, _) => properties.Add(basicProperties))
            .Returns(ValueTask.CompletedTask);

        var bus = CreateBus(channel.Object);

        var firstPublish = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "first" },
            TestContext.Current.CancellationToken
        );

        await Task.Yield();
        channel.Raise(
            c => c.BasicReturnAsync += null,
            new object(),
            new BasicReturnEventArgs(
                1,
                "NO_ROUTE",
                RabbitMqConfig.ExchangeName,
                RabbitMqConfig.ChatSend,
                new BasicProperties { MessageId = properties[0].MessageId },
                ReadOnlyMemory<byte>.Empty,
                CancellationToken.None
            )
        );
        channel.Raise(c => c.BasicAcksAsync += null, new object(), new BasicAckEventArgs(1, false));

        await Assert.ThrowsAsync<InvalidOperationException>(() => firstPublish);

        var secondPublish = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "second" },
            TestContext.Current.CancellationToken
        );

        await Task.Yield();
        channel.Raise(c => c.BasicAcksAsync += null, new object(), new BasicAckEventArgs(2, false));

        await secondPublish;
    }

    /// <summary>
    /// Broker NACK — терминальное отрицательное подтверждение и не должен
    /// превращаться в ожидание 30-секундного timeout.
    /// </summary>
    [Fact]
    public async Task BasicNackFailsTheMatchingPublish()
    {
        var channel = OpenChannel();
        var bus = CreateBus(channel.Object);

        var publishTask = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "nack" },
            TestContext.Current.CancellationToken
        );

        await Task.Yield();
        channel.Raise(
            c => c.BasicNacksAsync += null,
            new object(),
            new BasicNackEventArgs(1, false, false, CancellationToken.None)
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => publishTask);
    }

    /// <summary>
    /// Отмена со стороны вызывающего кода сохраняет OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task CallerCancellationRemainsOperationCanceled()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(instance => instance.IsOpen).Returns(true);
        channel
            .Setup(instance =>
                instance.GetNextPublishSequenceNumberAsync(It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(1UL);

        var publishStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
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
            >((_, _, _, _, _, _) => publishStarted.TrySetResult(true))
            .Returns(ValueTask.CompletedTask);

        var bus = CreateBus(channel.Object);
        using var cancellation = new CancellationTokenSource();

        var publishTask = bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "cancel" },
            cancellation.Token
        );

        await publishStarted.Task;
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publishTask);
        channel.Verify(instance => instance.DisposeAsync(), Times.Once);
    }

    /// <summary>
    /// Закрытие шины освобождает соединение и семафоры: иначе у процесса остались
    /// бы открытые сокеты и мёртвые семафоры.
    /// </summary>
    [Fact]
    public async Task DisposingReleasesConnection()
    {
        var connection = new Mock<IConnection>();
        var bus = CreateBus(new Mock<IChannel>().Object, connection.Object);

        await bus.DisposeAsync();

        connection.Verify(instance => instance.DisposeAsync(), Times.Once);
    }

    private static RabbitMqEventBus CreateBus(IChannel channel, IConnection? connection = null)
    {
        var bus = new RabbitMqEventBus(
            new RabbitMqOptions
            {
                Host = "localhost",
                UserName = "guest",
                Password = "guest",
            },
            "publisher",
            NullLogger<RabbitMqEventBus>.Instance
        );

        Set(bus, "_channel", channel);
        Set(bus, "_connection", connection);

        return bus;
    }

    private static Mock<IChannel> OpenChannel()
    {
        var channel = new Mock<IChannel>();
        channel.SetupGet(instance => instance.IsOpen).Returns(true);
        channel
            .Setup(instance =>
                instance.GetNextPublishSequenceNumberAsync(It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(1UL);
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
            .Returns(ValueTask.CompletedTask);
        return channel;
    }

    /// <summary>
    /// Соединение и канал подменяются рефлексией: конструктор создаёт настоящую
    /// фабрику, а создать соединение без брокера нельзя.
    /// </summary>
    private static void Set(object target, string field, object? value) =>
        target
            .GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);
}
