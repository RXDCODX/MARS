using System.Text.Json;
using System.Text.Json.Serialization;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Models.Overlay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Alerts.Api.Controllers;

/// <summary>
/// Внутренний межсервисный эндпоинт вброса события в оверлей.
/// </summary>
/// <remarks>
/// <para>
/// Доступен только с корректным <c>X-Api-Key</c> (политика <c>ServiceApiKey</c>).
/// Оверлеем владеет этот сервис: <see cref="EventBroadcaster{TMessage}"/> лежит
/// в его памяти, а сервисы, которые публикуют события оверлея, но им не владеют,
/// доставляют событие сюда.
/// </para>
/// <para>
/// Тело — JSON protobuf-сообщения <see cref="TelegramusEvent"/> как есть, а не
/// собственный DTO. Ветка <c>oneof</c> насчитывает 35 вариантов с <c>bytes</c>-
/// полями, и повторять её вторым контрактом значило бы поддерживать две схемы
/// одного события. Поэтому поле разбирает protobuf, а не MVC.
/// </para>
/// </remarks>
[ApiController]
[Route("api/internal/overlay")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class OverlayInternalController(
    EventBroadcaster<TelegramusEvent> broadcaster,
    ILogger<OverlayInternalController> logger
) : ControllerBase
{
    /// <summary>
    /// Рассылает событие подписчикам оверлея этого процесса.
    /// </summary>
    [HttpPost("events")]
    public async Task<ActionResult<OperationResult<OverlayFireResult>>> Fire(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken = default
    )
    {
        TelegramusEvent? notification;

        try
        {
            notification = JsonSerializer.Deserialize<TelegramusEvent>(
                payload.GetRawText(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                }
            );
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Событие оверлея не разобрано: некорректный JSON");

            return BadRequest(
                OperationResult<OverlayFireResult>.Fail(
                    $"Событие не разобрано: {exception.Message}"
                )
            );
        }

        if (notification is null)
        {
            return BadRequest(
                OperationResult<OverlayFireResult>.Fail("Событие не разобрано: пустое тело.")
            );
        }

        await broadcaster.BroadcastAsync(notification);

        logger.LogInformation(
            "Fire: событие {EventCase} разослано {Subscribers} подписчикам",
            notification.EventCase,
            broadcaster.SubscriberCount
        );

        return Ok(
            OperationResult<OverlayFireResult>.Ok(
                new OverlayFireResult { Subscribers = broadcaster.SubscriberCount }
            )
        );
    }
}
