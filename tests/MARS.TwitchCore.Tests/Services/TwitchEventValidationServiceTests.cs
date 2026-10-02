using MARS.TwitchCore.Services.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Точка входа валидации событий Twitch.
///
/// Через неё проходят обработчики наград, автоприветствий и переключателя
/// раскладки. Проверяется, что она выдаёт построитель проверок и не заводит
/// состояние на каждый вызов: сервисы поднимаются в любом порядке.
/// </summary>
public class TwitchEventValidationServiceTests
{
    [Fact]
    public void MessageValidationBuilderIsCreated()
    {
        var service = Create();

        var builder = service.ForMessageReceived(PassingValidationService.Message("привет"));

        Assert.NotNull(builder);
    }

    [Fact]
    public void RedemptionValidationBuilderIsCreated()
    {
        var service = Create();

        var builder = service.ForRedemption(
            new TwitchLib.EventSub.Core.EventArgs.Channel.ChannelPointsCustomRewardRedemptionArgs()
        );

        Assert.NotNull(builder);
    }

    /// <summary>
    /// Счётчик отправленных ошибок заводится на сервис: по нему видно, сколько раз
    /// событие отвергалось.
    /// </summary>
    [Fact]
    public void ErrorCounterIsCreatedOnce()
    {
        var service = Create();

        Assert.NotNull(SentEventErrors(service));
        Assert.Same(SentEventErrors(service), SentEventErrors(service));
    }

    private static object? SentEventErrors(TwitchEventValidationService service)
    {
        var field = typeof(TwitchEventValidationService).GetField(
            "SentEventErrors",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        return field?.GetValue(service);
    }

    private static TwitchEventValidationService Create() =>
        new(Mock.Of<ITwitchClient>(), NullLogger<TwitchEventValidationService>.Instance);
}
