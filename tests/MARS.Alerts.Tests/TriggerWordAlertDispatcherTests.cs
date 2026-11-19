using MARS.Alerts.Application.Alerts;
using MARS.Alerts.Application.TriggerWords;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests;

/// <summary>
/// Проверяет поведение диспетчера ключевых слов при доступном и недоступном хранилище.
/// Сами правила сопоставления проверяются в TriggerWordMatcherTests.
/// </summary>
public class TriggerWordAlertDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_LoadsAlertsForChatMessage()
    {
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaInfo>?)Array.Empty<MediaInfo>());
        var dispatcher = new TriggerWordAlertDispatcher(
            source.Object,
            new EventBroadcaster<TelegramusEvent>(),
            NullLogger<TriggerWordAlertDispatcher>.Instance
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchAsync(
            new ChatEventMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "всем привет",
            },
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ReturnsWhenStorageIsUnavailable()
    {
        var source = new Mock<IEnabledAlertSource>();
        source
            .Setup(x => x.GetEnabledAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaInfo>?)null);
        var dispatcher = new TriggerWordAlertDispatcher(
            source.Object,
            new EventBroadcaster<TelegramusEvent>(),
            NullLogger<TriggerWordAlertDispatcher>.Instance
        );
        var ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchAsync(
            new ChatEventMessageEvent
            {
                UserId = "42",
                UserName = "Viewer",
                Message = "всем привет",
            },
            ct
        );

        source.Verify(x => x.GetEnabledAlertsAsync(ct), Times.Once);
    }
}
