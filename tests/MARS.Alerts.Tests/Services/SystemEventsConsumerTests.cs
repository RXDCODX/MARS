using System.Reflection;
using MARS.Alerts.Services;
using MARS.Shared.Configuration;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Потребитель системных событий.
///
/// Проверяется, что событие мужа разводит только взрыв во вкладке гейм-баффера,
/// а коллекции (Fumo/Miku/Frog) не показывают ничего: показывать нечего.
/// </summary>
public class SystemEventsConsumerTests
{
    private readonly Mock<ITelegramusNotifier> _notifier = new();
    private readonly SystemEventsConsumer _consumer = new(
        Options.Create(new RabbitMqOptions()),
        Mock.Of<ITelegramusNotifier>(),
        NullLogger<SystemEventsConsumer>.Instance
    );

    public SystemEventsConsumerTests() =>
        _consumer = new SystemEventsConsumer(
            Options.Create(new RabbitMqOptions()),
            _notifier.Object,
            NullLogger<SystemEventsConsumer>.Instance
        );

    /// <summary>
    /// Результат рулетки во вторую секунду показывается взрывом — и только им.
    /// </summary>
    [Fact]
    public async Task WaifuRollResultTriggersExplosion()
    {
        await HandleAsync(RabbitMqConfig.WaifuRollResult, "{}");

        _notifier.Verify(instance => instance.Explosion(), Times.Once);
    }

    /// <summary>
    /// Сбор коллекции не показывает оверлей: у коллекции своя полоса в интерфейсе,
    /// а всплывающая плашка лишняя.
    /// </summary>
    [Theory]
    [InlineData("waifu.fumo.result")]
    [InlineData("waifu.miku.result")]
    [InlineData("waifu.frog.result")]
    public async Task CollectionResultsAreSilent(string routingKey)
    {
        await HandleAsync(routingKey, "{}");

        _notifier.Verify(instance => instance.Explosion(), Times.Never);
        _notifier.Verify(instance => instance.FumoFriday(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Событие дорожки только логируется: переключение трека не должно дёргать
    /// оверлей зрителя.
    /// </summary>
    [Theory]
    [InlineData("media.track.started")]
    [InlineData("media.track.ended")]
    [InlineData("media.track.added")]
    public async Task TrackEventsAreOnlyLogged(string routingKey)
    {
        await HandleAsync(routingKey, "{}");

        _notifier.Verify(instance => instance.Explosion(), Times.Never);
        _notifier.Verify(instance => instance.AllRefund(It.IsAny<object>()), Times.Never);
    }

    /// <summary>
    /// Неизвестный ключ не роняет потребителя: подписка живёт до перезапуска, и
    /// одно лишнее событие не должно уводить её в DLQ.
    /// </summary>
    [Fact]
    public async Task UnknownRoutingKeyIsIgnored()
    {
        await HandleAsync("что-то новое", "{}");

        _notifier.Verify(instance => instance.Explosion(), Times.Never);
    }

    private Task HandleAsync(string routingKey, string json)
    {
        var method = typeof(SystemEventsConsumer).GetMethod(
            "HandleMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(_consumer, [routingKey, json, CancellationToken.None])!;
    }
}
