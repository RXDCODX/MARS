using System.Reflection;
using MARS.TwitchCore.Services.EventSub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Служба подписок EventSub.
///
/// Проверяется поведение без токена: подписаться без токена нельзя, и сервис
/// обязан сообщить об этом строкой, а не бросить исключение наружу — вызывающий
/// код печатает этот результат в чат как отчёт о переподключении. Само
/// переподключение к WebSocket Twitch здесь не проверяется: клиент уходит в
/// сеть, и такой тест проверял бы не код, а доступность Twitch.
/// </summary>
public class EventSubServiceTests
{
    [Fact]
    public void WebSocketIsNotConnectedWithoutSession()
    {
        var service = Create();

        Assert.False(service.IsWebSocketConnected);
    }

    [Fact]
    public async Task SubscriptionsAreNotRequestedWithoutToken()
    {
        var service = Create();

        Assert.Null(await service.GetEventSubsAsync());
    }

    /// <summary>
    /// Переподключение без токена возвращает текст ошибки, а не падает: вызов
    /// идёт из фонового таймера, и исключение там остановило бы всю службу.
    /// </summary>
    [Fact]
    public async Task ResubscribeWithoutTokenReportsError()
    {
        var service = Create();

        var result = await service.ResubscribeToEventSubAsync();

        Assert.StartsWith("Ошибка", result);
    }

    /// <summary>
    /// Удаление подписок без токена тоже не должно ходить в Twitch: иначе первый
    /// же запуск без токена упал бы на пустом Helix.
    /// </summary>
    [Fact]
    public async Task DeletingSubscriptionsWithoutTokenIsSafe()
    {
        var service = Create();

        await InvokeAsync(service, "DeleteAllSubsAsync");
    }

    /// <summary>
    /// Список подписок пуст, а не «прочитан»: без токена читать нечего.
    /// </summary>
    [Fact]
    public async Task NoSubscriptionsWithoutToken()
    {
        var service = Create();

        Assert.Null((await service.GetEventSubsAsync())?.Subscriptions);
    }

    private static EventSubService Create() =>
        new(
            Mock.Of<ITwitchAPI>(),
            NullLogger<EventSubService>.Instance,
            TwitchTokenServiceStub.WithoutToken(),
            new TestLifetime(),
            OfflineEventSub.Create()
        );

    private static async Task InvokeAsync(EventSubService service, string method)
    {
        var target = typeof(EventSubService).GetMethod(
            method,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)target.Invoke(service, null)!;
    }
}
