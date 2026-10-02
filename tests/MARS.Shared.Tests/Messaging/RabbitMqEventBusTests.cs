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
    /// Событие уходит в обмен с ключом маршрутизации и метками сервиса: по ним
    /// потребитель понимает, от кого пришло событие.
    /// </summary>
    [Fact]
    public async Task EventIsPublishedWithServiceHeaders()
    {
        var channel = OpenChannel();
        var bus = CreateBus(channel.Object);

        await bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "привет" },
            TestContext.Current.CancellationToken
        );

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

        await bus.PublishAsync(
            RabbitMqConfig.ChatSend,
            new { Text = "привет" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, attempts);
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
