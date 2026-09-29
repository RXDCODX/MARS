using MARS.Shared.Models;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomRewardRedemption;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/twitch/rewards")]
public class TwitchRewardsController(
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<TwitchRewardsController> logger
) : ControllerBase
{
    private string? AccessToken => tokenService.Token!.AccessToken;

    [HttpGet]
    public async Task<ActionResult<OperationResult<GetCustomRewardsResponse?>>> GetRewards(
        [FromQuery] bool onlyManageable = true
    )
    {
        ActionResult<OperationResult<GetCustomRewardsResponse?>> result;
        try
        {
            var response = await api.Helix.ChannelPoints.GetCustomRewardAsync(
                TwitchConstants.ChannelId,
                null,
                onlyManageable,
                AccessToken
            );
            result = Ok(OperationResult<GetCustomRewardsResponse?>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<GetCustomRewardsResponse?>.Fail("Не удалось получить награды")
            );
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<CustomReward?>>> CreateReward(
        [FromBody] CreateCustomRewardsRequest request
    )
    {
        ActionResult<OperationResult<CustomReward?>> result;
        try
        {
            var created = await api.Helix.ChannelPoints.CreateCustomRewardsAsync(
                TwitchConstants.ChannelId,
                request,
                AccessToken
            );
            var reward = created.Data.FirstOrDefault();
            result = Ok(OperationResult<CustomReward?>.Ok(reward));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<CustomReward?>.Fail("Не удалось создать награду"));
        }

        return result;
    }

    [HttpPatch("{rewardId}")]
    public async Task<ActionResult<OperationResult<CustomReward?>>> UpdateReward(
        string rewardId,
        [FromBody] UpdateCustomRewardRequest request
    )
    {
        ActionResult<OperationResult<CustomReward?>> result;
        try
        {
            var updated = await api.Helix.ChannelPoints.UpdateCustomRewardAsync(
                TwitchConstants.ChannelId,
                rewardId,
                request,
                AccessToken
            );
            var reward = updated.Data.FirstOrDefault();
            result = Ok(OperationResult<CustomReward?>.Ok(reward));
        }
        catch (TwitchLib.Api.Core.Exceptions.BadRequestException bre)
        {
            logger.LogWarning(bre, "BadRequest при обновлении награды {RewardId}", rewardId);
            result = Ok(OperationResult<CustomReward?>.Fail(bre.Message));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult<CustomReward?>.Fail("Не удалось обновить награду"));
        }

        return result;
    }

    [HttpDelete("{rewardId}")]
    public async Task<ActionResult<OperationResult>> DeleteReward(string rewardId)
    {
        ActionResult<OperationResult> result;
        try
        {
            await api.Helix.ChannelPoints.DeleteCustomRewardAsync(
                TwitchConstants.ChannelId,
                rewardId,
                AccessToken
            );
            result = Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult.Fail("Не удалось удалить награду"));
        }

        return result;
    }

    [HttpGet("{rewardId}/redemptions")]
    public async Task<
        ActionResult<OperationResult<GetCustomRewardRedemptionResponse?>>
    > GetRedemptions(
        string rewardId,
        [FromQuery] string status = "UNFULFILLED",
        [FromQuery] string? sort = null,
        [FromQuery] string? after = null
    )
    {
        ActionResult<OperationResult<GetCustomRewardRedemptionResponse?>> result;
        try
        {
            var response = await api.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(
                TwitchConstants.ChannelId,
                rewardId,
                [status],
                sort,
                after,
                AccessToken
            );
            result = Ok(OperationResult<GetCustomRewardRedemptionResponse?>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(
                OperationResult<GetCustomRewardRedemptionResponse?>.Fail(
                    "Не удалось получить редемпшены"
                )
            );
        }

        return result;
    }

    [HttpPost("{rewardId}/redemptions/status")]
    public async Task<ActionResult<OperationResult>> UpdateRedemptionStatus(
        string rewardId,
        [FromBody] UpdateCustomRewardRedemptionStatusRequest request,
        [FromQuery] List<string> ids
    )
    {
        ActionResult<OperationResult> result;
        try
        {
            if (ids == null || ids.Count == 0)
            {
                result = Ok(OperationResult.Fail("Не указаны идентификаторы редемпшенов"));
            }
            else
            {
                await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                    TwitchConstants.ChannelId,
                    rewardId,
                    ids,
                    request,
                    AccessToken
                );
                result = Ok(OperationResult.Ok());
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            result = Ok(OperationResult.Fail("Не удалось обновить статус редемпшенов"));
        }

        return result;
    }
}
