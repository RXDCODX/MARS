using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MARS.Alerts.Api.Controllers;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Models.Overlay;
using MARS.Shared.Contracts.Telegramus;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Controllers;

/// <summary>
/// Внутренний REST-эндпоинт вброса события в оверлей.
/// </summary>
/// <remarks>
/// Тело — JSON protobuf-сообщения, а не собственный DTO: ветка <c>oneof</c>
/// насчитывает 35 вариантов с <c>bytes</c>-полями, и вторая схема того же события
/// разошлась бы с первой. Поэтому проверяется, что protobuf разбирает тело сам.
/// </remarks>
public class OverlayInternalControllerTests
{
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = new();

    [Fact]
    public async Task Событие_Разбирается_И_Доходит_До_Подписчика()
    {
        using var subscription = _broadcaster.Subscribe();
        var payload = JsonFormatter.Default.Format(
            new TelegramusEvent { Explosion = new EmptyEvent() }
        );
        using var document = JsonDocument.Parse(payload);

        var result = await Create()
            .Fire(document.RootElement, TestContext.Current.CancellationToken);

        var envelope = Assert.IsType<OperationResult<OverlayFireResult>>(
            Assert.IsType<OkObjectResult>(result.Result).Value
        );

        Assert.True(envelope.Success);
        Assert.Equal(1, envelope.Result!.Subscribers);

        Assert.True(subscription.Reader.TryRead(out var notification));
        Assert.Equal(TelegramusEvent.EventOneofCase.Explosion, notification!.EventCase);
    }

    /// <summary>
    /// Неизвестное поле — это ошибка вызывающего, а не повод рассылать половину
    /// события: 400 говорит отправителю, что тело собрано не по схеме.
    /// </summary>
    [Fact]
    public async Task Некорректное_Событие_Отклоняется_Статусом_400()
    {
        using var document = JsonDocument.Parse("""{"noSuchField":1}""");

        var result = await Create()
            .Fire(document.RootElement, TestContext.Current.CancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<OperationResult<OverlayFireResult>>(bad.Value).Success);
    }

    private OverlayInternalController Create() =>
        new(_broadcaster, NullLogger<OverlayInternalController>.Instance);
}
