using MARS.Shared.Models;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.ChannelRewards;
using Microsoft.AspNetCore.Mvc;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelRewardsManagerController(
    ChannelRewardsManager manager,
    ILogger<ChannelRewardsManagerController> logger,
    ChannelRewardsSyncService syncService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<IEnumerable<ChannelRewardRecord>>>> GetAll()
    {
        ActionResult<OperationResult<IEnumerable<ChannelRewardRecord>>> result;
        try
        {
            var rewards = await manager.GetAllAsync();
            result = Ok(
                OperationResult<IEnumerable<ChannelRewardRecord>>.Ok(
                    rewards?.Select(r => new ChannelRewardRecord
                    {
                        Title = r.Title,
                        Cost = r.Cost,
                        IsEnabled = r.IsEnabled,
                        TwitchRewardId = r.Id,
                    }) ?? []
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<IEnumerable<ChannelRewardRecord>>.Fail(
                    "Ошибка при получении наград"
                )
            );
        }

        return result;
    }

    [HttpGet("local")]
    public async Task<ActionResult<OperationResult<IEnumerable<ChannelRewardRecord>>>> GetAllLocal()
    {
        ActionResult<OperationResult<IEnumerable<ChannelRewardRecord>>> result;
        try
        {
            var rewards = await manager.GetLocalAsync();
            result = Ok(OperationResult<IEnumerable<ChannelRewardRecord>>.Ok(rewards));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<IEnumerable<ChannelRewardRecord>>.Fail(
                    "Ошибка при получении локальных наград"
                )
            );
        }

        return result;
    }

    [HttpGet("local/{localId:guid}")]
    public async Task<ActionResult<OperationResult<ChannelRewardRecord?>>> GetLocalById(
        [FromRoute] Guid localId
    )
    {
        ActionResult<OperationResult<ChannelRewardRecord?>> result;
        try
        {
            var reward = await manager.GetLocalByIdAsync(localId);

            if (reward != null)
            {
                result = Ok(OperationResult<ChannelRewardRecord?>.Ok(reward));
            }
            else
            {
                result = Ok(
                    OperationResult<ChannelRewardRecord?>.Fail(
                        $"Локальная награда с ID {localId} не найдена"
                    )
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<ChannelRewardRecord?>.Fail("Ошибка при получении локальной награды")
            );
        }

        return result;
    }

    [HttpGet("{rewardId}")]
    public async Task<ActionResult<OperationResult<ChannelRewardRecord?>>> GetById(
        [FromRoute] string rewardId
    )
    {
        ActionResult<OperationResult<ChannelRewardRecord?>> result;
        try
        {
            var reward = await manager.GetByIdAsync(rewardId);

            if (reward != null)
            {
                result = Ok(
                    OperationResult<ChannelRewardRecord?>.Ok(
                        new ChannelRewardRecord
                        {
                            Title = reward.Title,
                            Cost = reward.Cost,
                            IsEnabled = reward.IsEnabled,
                            TwitchRewardId = reward.Id,
                        }
                    )
                );
            }
            else
            {
                result = Ok(
                    OperationResult<ChannelRewardRecord?>.Fail(
                        $"Награда с ID {rewardId} не найдена"
                    )
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<ChannelRewardRecord?>.Fail("Ошибка при получении награды"));
        }

        return result;
    }

    [HttpPost("local")]
    public async Task<ActionResult<OperationResult<ChannelRewardRecord?>>> UpsertLocal(
        [FromBody] ChannelRewardDefinition definition
    )
    {
        ActionResult<OperationResult<ChannelRewardRecord?>> result;
        try
        {
            var record = await manager.UpsertLocalAsync(definition);

            if (record != null)
            {
                result = Ok(OperationResult<ChannelRewardRecord?>.Ok(record));
            }
            else
            {
                result = Ok(
                    OperationResult<ChannelRewardRecord?>.Fail(
                        "Не удалось сохранить локальную награду"
                    )
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<ChannelRewardRecord?>.Fail(
                    $"Ошибка при сохранении локальной награды: {ex.Message}"
                )
            );
        }

        return result;
    }

    [HttpPut("local/{localId:guid}")]
    public async Task<ActionResult<OperationResult>> UpdateLocal(
        [FromRoute] Guid localId,
        [FromBody] UpdateCustomRewardDto dto
    )
    {
        ActionResult<OperationResult> result;
        try
        {
            var ok = await manager.UpdateLocalAsync(localId, dto);

            if (ok)
            {
                result = Ok(OperationResult.Ok());
            }
            else
            {
                result = Ok(OperationResult.Fail("Не удалось обновить награду"));
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult.Fail("Ошибка при обновлении локальной награды"));
        }

        return result;
    }

    [HttpDelete("local/{localId:guid}")]
    public async Task<ActionResult<OperationResult>> SoftDeleteLocal([FromRoute] Guid localId)
    {
        ActionResult<OperationResult> result;
        try
        {
            var ok = await manager.SoftDeleteLocalAsync(localId);

            if (ok)
            {
                result = Ok(OperationResult.Ok());
            }
            else
            {
                result = Ok(OperationResult.Fail("Не удалось удалить награду"));
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult.Fail("Ошибка при удалении локальной награды"));
        }

        return result;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<OperationResult>> SyncNow(CancellationToken cancellationToken)
    {
        ActionResult<OperationResult> result;
        try
        {
            await syncService.SyncNow(cancellationToken);
            result = Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult.Fail($"Ошибка синхронизации: {ex.Message}"));
        }

        return result;
    }

    [HttpPost("sync-services")]
    public async Task<ActionResult<OperationResult<int>>> SyncServicesToLocal()
    {
        ActionResult<OperationResult<int>> result;
        try
        {
            var count = await manager.SyncRewardServicesToLocalAsync();
            result = Ok(OperationResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<int>.Fail($"Ошибка синхронизации сервисов: {ex.Message}"));
        }

        return result;
    }
}
