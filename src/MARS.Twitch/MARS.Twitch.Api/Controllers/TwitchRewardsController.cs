using MARS.Shared.Contracts.Models;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomRewardRedemption;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Api.Controllers;

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
    public ActionResult<OperationResult<CustomReward?>> CreateReward(
        [FromBody] CreateCustomRewardsRequest request
    ) =>
        BadRequest(
            OperationResult<CustomReward?>.Fail(
                "Произвольное создание наград отключено; награды управляются MARS.Alerts"
            )
        );

    [HttpPatch("{rewardId}")]
    public ActionResult<OperationResult<CustomReward?>> UpdateReward(
        string rewardId,
        [FromBody] UpdateCustomRewardRequest request
    ) =>
        BadRequest(
            OperationResult<CustomReward?>.Fail(
                "Прямое изменение наград отключено; используйте локальные настройки MARS"
            )
        );

    [HttpDelete("{rewardId}")]
    public ActionResult<OperationResult> DeleteReward(string rewardId) =>
        BadRequest(
            OperationResult.Fail(
                "Прямое удаление наград отключено; используйте отключение локальной записи"
            )
        );

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
