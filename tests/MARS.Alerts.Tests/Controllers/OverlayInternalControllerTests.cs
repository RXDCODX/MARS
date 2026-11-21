using System.Text.Json;
using MARS.Alerts.Api.Controllers;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Models.Overlay;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Alerts.Tests.Controllers;

/// <summary>
/// Внутренний REST-эндпоинт вброса события в оверлей.
/// </summary>
public class OverlayInternalControllerTests
{
    private readonly TestOverlayHub _overlay = new();

    [Fact]
    public async Task Событие_Разбирается_И_Доходит_До_Подписчика()
    {
        var payload = JsonSerializer.Serialize(
            new TelegramusEvent { Explosion = new EmptyEvent() },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        using var document = JsonDocument.Parse(payload);

        var result = await Create()
            .Fire(document.RootElement, TestContext.Current.CancellationToken);

        var envelope = Assert.IsType<OperationResult<OverlayFireResult>>(
            Assert.IsType<OkObjectResult>(result.Result).Value
        );

        Assert.True(envelope.Success);
        Assert.Equal(1, envelope.Result!.Subscribers);
        _overlay.ClientsMock.Verify(c => c.Explosion(), Times.Once);
    }

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
        new(_overlay.Context, NullLogger<OverlayInternalController>.Instance);
}
