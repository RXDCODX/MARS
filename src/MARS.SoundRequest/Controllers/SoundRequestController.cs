using MARS.Shared.Models;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.YouTube;
using Microsoft.AspNetCore.Mvc;

namespace MARS.SoundRequest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoundRequestController(
    MainPlayer player,
    SoundRequestCommandsService service,
    SoundRequestUserQueue queue,
    ILogger<SoundRequestController> logger
) : ControllerBase
{
    /// <summary>
    /// Получить состояние плеера
    /// </summary>
    [HttpGet("state")]
    public ActionResult<OperationResult<PlayerState>> GetPlayerState()
    {
        ActionResult<OperationResult<PlayerState>> result;

        try
        {
            var state = player.GetState();
            result = Ok(OperationResult<PlayerState>.Ok(state));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении состояния плеера");
            result = Ok(OperationResult<PlayerState>.Fail("Ошибка при получении состояния плеера"));
        }

        return result;
    }

    /// <summary>
    /// Получить очередь элементов
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult<OperationResult<List<QueueItem>>>> GetQueue(
        CancellationToken ct = default
    )
    {
        ActionResult<OperationResult<List<QueueItem>>> result;

        try
        {
            var queueItems = await player.GetQueueAsync();
            result = Ok(OperationResult<List<QueueItem>>.Ok(queueItems));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении очереди");
            result = Ok(OperationResult<List<QueueItem>>.Fail("Ошибка при получении очереди"));
        }

        return result;
    }

    /// <summary>
    /// Получить историю воспроизведенных треков
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<OperationResult<List<BaseTrackInfo>>>> GetHistory(
        [FromQuery] int count = 20,
        CancellationToken ct = default
    )
    {
        ActionResult<OperationResult<List<BaseTrackInfo>>> result;

        try
        {
            var history = await player.GetHistoryAsync(count);
            result = Ok(OperationResult<List<BaseTrackInfo>>.Ok(history));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении истории");
            result = Ok(OperationResult<List<BaseTrackInfo>>.Fail("Ошибка при получении истории"));
        }

        return result;
    }

    /// <summary>
    /// Получить историю воспроизведенных треков как элементы очереди
    /// </summary>
    [HttpGet("history/queue-items")]
    public async Task<ActionResult<OperationResult<List<QueueItem>>>> GetHistoryQueueItems(
        [FromQuery] int count = 20,
        CancellationToken ct = default
    )
    {
        ActionResult<OperationResult<List<QueueItem>>> result;

        try
        {
            var historyItems = await player.GetHistoryQueueItemsAsync(count);
            result = Ok(OperationResult<List<QueueItem>>.Ok(historyItems));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении истории как QueueItem");
            result = Ok(OperationResult<List<QueueItem>>.Fail("Ошибка при получении истории"));
        }

        return result;
    }

    /// <summary>
    /// Получить текущую или последнюю проигранную песню
    /// </summary>
    [HttpGet("current-song")]
    public async Task<ActionResult<OperationResult<string>>> GetCurrentSong(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string>> result;

        try
        {
            var message = await service.GetCurrentSongAsync(cancellationToken);
            result = Ok(OperationResult<string>.Ok(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении текущей песни");
            result = Ok(OperationResult<string>.Fail("Ошибка при получении текущей песни"));
        }

        return result;
    }

    /// <summary>
    /// Удалить элемент из очереди
    /// </summary>
    [HttpDelete("queue/{queueItemId}")]
    public async Task<ActionResult<OperationResult>> DeleteFromQueue(
        [FromRoute] Guid queueItemId,
        CancellationToken ct = default
    )
    {
        ActionResult<OperationResult> result;

        try
        {
            if (queueItemId != Guid.Empty)
            {
                var queueItem = await queue.GetQueueItemByIdAsync(queueItemId);

                if (queueItem != null)
                {
                    await queue.RemoveFromQueueAsync(queueItemId);
                    result = Ok(OperationResult.Ok());
                }
                else
                {
                    result = Ok(OperationResult.Fail("Элемент не найден в очереди"));
                }
            }
            else
            {
                result = Ok(OperationResult.Fail("Некорректный идентификатор элемента"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении элемента из очереди: Id={Id}", queueItemId);
            result = Ok(OperationResult.Fail("Ошибка при удалении элемента из очереди"));
        }

        return result;
    }

    /// <summary>
    /// Добавить трек в очередь
    /// </summary>
    [HttpPost("add-track")]
    public async Task<ActionResult<OperationResult<string>>> AddTrack(
        [FromQuery] string query,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string>> result;

        try
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                var processedQuery = query.Trim();
                if (processedQuery.StartsWith("!sr", StringComparison.OrdinalIgnoreCase))
                {
                    processedQuery = processedQuery.Substring(3).TrimStart();
                }

                var message = await service.AddTrackAsync(
                    processedQuery,
                    "web-user",
                    cancellationToken
                );

                result = Ok(OperationResult<string>.Ok(message));
            }
            else
            {
                result = Ok(
                    OperationResult<string>.Fail("Необходимо указать URL или название трека")
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении трека в очередь: Query={Query}", query);
            result = Ok(OperationResult<string>.Fail("Ошибка при добавлении трека в очередь"));
        }

        return result;
    }

    /// <summary>
    /// Немедленно воспроизвести трек из очереди
    /// </summary>
    [HttpPost("play-now/{queueItemId}")]
    public async Task<ActionResult<OperationResult<string>>> PlayQueueItemNow(
        [FromRoute] Guid queueItemId,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string>> result;

        try
        {
            if (queueItemId != Guid.Empty)
            {
                var message = await service.PlayQueueItemNowAsync(queueItemId, cancellationToken);
                result = Ok(OperationResult<string>.Ok(message));
            }
            else
            {
                result = Ok(OperationResult<string>.Fail("Некорректный идентификатор элемента"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при немедленном воспроизведении трека: QueueItemId={QueueItemId}",
                queueItemId
            );
            result = Ok(OperationResult<string>.Fail("Ошибка при воспроизведении трека"));
        }

        return result;
    }

    public class QueueReorderRequest
    {
        public Guid QueueItemId { get; set; }
        public int NewPosition { get; set; }
    }

    /// <summary>
    /// Поменять позицию элемента в очереди
    /// </summary>
    [HttpPost("queue/reorder")]
    public async Task<ActionResult<OperationResult<string>>> ReorderQueueItem(
        [FromBody] QueueReorderRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string>> result;

        try
        {
            if (request == null || request.QueueItemId == Guid.Empty)
            {
                result = Ok(OperationResult<string>.Fail("Некорректный запрос"));
            }
            else
            {
                var message = await service.ReorderQueueItemAsync(
                    request.QueueItemId,
                    request.NewPosition,
                    cancellationToken
                );

                result = Ok(OperationResult<string>.Ok(message));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при перестановке элемента очереди: Id={Id}",
                request?.QueueItemId
            );
            result = Ok(OperationResult<string>.Fail("Ошибка при перестановке элемента очереди"));
        }

        return result;
    }
}
