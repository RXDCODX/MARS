using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Kernel;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.MiniGamesStats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Api.Controllers;

/// <summary>
/// Internal-эндпоинты межсервисных операций MARS.Twitch (VIP, возврат баллов,
/// результаты мини-игр). Закрыты политикой ServiceApiKey. Чат отправляется
/// через <see cref="InternalController"/> (api/internal/chat/send).
/// </summary>
[ApiController]
[Route("api/internal")]
[Authorize(Policy = "ServiceApiKey")]
public class InternalTwitchController(
    ITwitchAPI api,
    TokenService tokenService,
    ILeaderboardService leaderboardService,
    ILogger<InternalTwitchController> logger
) : ControllerBase
{
    [HttpPost("vip/grant")]
    public async Task<ActionResult> GrantVip(
        [FromBody] TwitchVipGrantRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserId))
        {
            return Ok(OperationResult.Ok());
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("VIP grant: no access token available");
            return Ok(OperationResult.Fail("no access token"));
        }

        try
        {
            await api.Helix.Channels.AddChannelVIPAsync(
                TwitchConstants.ChannelId,
                request.UserId,
                accessToken
            );
            logger.LogInformation("VIP granted to user {UserId}", request.UserId);
            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Ok(OperationResult.Fail("vip grant failed"));
        }
    }

    [HttpPost("rewards/refund")]
    public async Task<ActionResult> RefundReward(
        [FromBody] RewardRefundRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (
            request is null
            || string.IsNullOrWhiteSpace(request.RewardId)
            || request.RedemptionIds is null
            || request.RedemptionIds.Count == 0
        )
        {
            logger.LogWarning("Reward refund has no reward/redemption ids, skipped");
            return Ok(OperationResult.Ok());
        }

        var accessToken = tokenService.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("Reward refund: no access token available");
            return Ok(OperationResult.Fail("no access token"));
        }

        try
        {
            await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                TwitchConstants.ChannelId,
                request.RewardId,
                request.RedemptionIds,
                new UpdateCustomRewardRedemptionStatusRequest
                {
                    Status = TwitchLib.Api.Core.Enums.CustomRewardRedemptionStatus.CANCELED,
                },
                accessToken
            );

            logger.LogInformation(
                "Refunded {Count} redemptions of reward {RewardId}: {Reason}",
                request.RedemptionIds.Count,
                request.RewardId,
                request.Reason
            );
            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Ok(OperationResult.Fail("refund failed"));
        }
    }

    [HttpPost("minigames/result")]
    public async Task<ActionResult> RecordMiniGameResult(
        [FromBody] MiniGameResultRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserId))
        {
            logger.LogWarning("MiniGame result has no user id, skipped");
            return Ok(OperationResult.Ok());
        }

        try
        {
            switch (request.Game)
            {
                case "russian_roulette":
                    await leaderboardService.RecordRouletteWinAsync(
                        request.UserId,
                        request.WithWaifu,
                        cancellationToken
                    );
                    break;
                case "quiz":
                    await leaderboardService.RecordTriviaWinAsync(
                        request.UserId,
                        request.WithWaifu,
                        cancellationToken
                    );
                    break;
                case "audio_quiz":
                    logger.LogInformation("AudioQuiz win by {UserName}", request.UserName);
                    break;
            }
            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record mini-game result");
            return Ok(OperationResult.Fail("minigame result failed"));
        }
    }
}
