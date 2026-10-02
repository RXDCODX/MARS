using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.CinemaQueue.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CinemaQueueController(
    ICinemaQueueService cinemaQueueService,
    ILogger<CinemaQueueController> logger
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<List<CinemaMediaItemDto>>>> GetAllMediaItems(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var items = await cinemaQueueService.GetAllMediaItemsAsync(cancellationToken);
            return Ok(OperationResult<List<CinemaMediaItemDto>>.Ok(items.ToList()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all media items");
            return Ok(OperationResult<List<CinemaMediaItemDto>>.Fail("Error getting media items"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperationResult<CinemaMediaItemDto?>>> GetMediaItem(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var item = await cinemaQueueService.GetMediaItemByIdAsync(id, cancellationToken);
            return item != null
                ? Ok(OperationResult<CinemaMediaItemDto?>.Ok(item))
                : Ok(OperationResult<CinemaMediaItemDto?>.Fail("Media item not found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting media item with ID: {Id}", id);
            return Ok(OperationResult<CinemaMediaItemDto?>.Fail("Error getting media item"));
        }
    }

    [HttpGet("next")]
    public async Task<ActionResult<OperationResult<CinemaMediaItemDto?>>> GetNextMediaItem(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var item = await cinemaQueueService.GetNextMediaItemAsync(cancellationToken);
            return item != null
                ? Ok(OperationResult<CinemaMediaItemDto?>.Ok(item))
                : Ok(OperationResult<CinemaMediaItemDto?>.Fail("No next media item found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting next media item");
            return Ok(OperationResult<CinemaMediaItemDto?>.Fail("Error getting next media item"));
        }
    }

    [HttpGet("status/{status}")]
    public async Task<
        ActionResult<OperationResult<List<CinemaMediaItemDto>>>
    > GetMediaItemsByStatus(MediaStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await cinemaQueueService.GetMediaItemsByStatusAsync(
                status,
                cancellationToken
            );
            return Ok(OperationResult<List<CinemaMediaItemDto>>.Ok(items.ToList()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting media items by status: {Status}", status);
            return Ok(
                OperationResult<List<CinemaMediaItemDto>>.Fail(
                    "Error getting media items by status"
                )
            );
        }
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<CinemaMediaItemDto?>>> CreateMediaItem(
        [FromBody] CreateMediaItemRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.MediaUrl))
            {
                return Ok(OperationResult<CinemaMediaItemDto?>.Fail("MediaUrl is required"));
            }

            var mediaItem = await cinemaQueueService.CreateMediaItemAsync(
                request,
                cancellationToken
            );
            return Ok(OperationResult<CinemaMediaItemDto?>.Ok(mediaItem));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating media item: {Title}", request.Title);
            return Ok(OperationResult<CinemaMediaItemDto?>.Fail("Error creating media item"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OperationResult<CinemaMediaItemDto?>>> UpdateMediaItem(
        Guid id,
        [FromBody] UpdateMediaItemRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var mediaItem = await cinemaQueueService.UpdateMediaItemAsync(
                id,
                request,
                cancellationToken
            );

            return mediaItem != null
                ? Ok(OperationResult<CinemaMediaItemDto?>.Ok(mediaItem))
                : Ok(OperationResult<CinemaMediaItemDto?>.Fail("Media item not found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating media item with ID: {Id}", id);
            return Ok(OperationResult<CinemaMediaItemDto?>.Fail("Error updating media item"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<OperationResult>> DeleteMediaItem(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var deleteResult = await cinemaQueueService.DeleteMediaItemAsync(id, cancellationToken);
            return deleteResult
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Media item not found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting media item with ID: {Id}", id);
            return Ok(OperationResult.Fail("Error deleting media item"));
        }
    }

    [HttpPost("{id:guid}/mark-as-next")]
    public async Task<ActionResult<OperationResult>> MarkAsNext(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await cinemaQueueService.MarkAsNextAsync(id, cancellationToken);
            return result
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Media item not found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking media item as next with ID: {Id}", id);
            return Ok(OperationResult.Fail("Error marking media item as next"));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OperationResult>> ChangeStatus(
        Guid id,
        [FromBody] MediaStatus status,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await cinemaQueueService.ChangeStatusAsync(id, status, cancellationToken);
            return result
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Media item not found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing status of media item with ID: {Id}", id);
            return Ok(OperationResult.Fail("Error changing status"));
        }
    }

    [HttpPatch("{id:guid}/priority")]
    public async Task<ActionResult<OperationResult>> ChangePriority(
        Guid id,
        [FromBody] int priority,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await cinemaQueueService.ChangePriorityAsync(
                id,
                priority,
                cancellationToken
            );
            return result
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Media item not found"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing priority of media item with ID: {Id}", id);
            return Ok(OperationResult.Fail("Error changing priority"));
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<OperationResult<CinemaQueueStatistics?>>> GetStatistics(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var stats = await cinemaQueueService.GetStatisticsAsync(cancellationToken);
            return Ok(OperationResult<CinemaQueueStatistics?>.Ok(stats));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting cinema queue statistics");
            return Ok(OperationResult<CinemaQueueStatistics?>.Fail("Error getting statistics"));
        }
    }
}
