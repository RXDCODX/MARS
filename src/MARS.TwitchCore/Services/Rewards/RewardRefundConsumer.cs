using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Options;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Возврат баллов канала за редемпшн, который MARS.Alerts отклонил.
/// </summary>
/// <remarks>
/// <para>
/// <c>UpdateRedemptionStatus</c> требует токена стримера, а он есть только у
/// MARS.TwitchCore. MARS.Alerts публикует просьбу в
/// <see cref="RabbitMqConfig.RewardRefund"/>, а возврат исполняет этот
/// потребитель: второй клиент Twitch API в Alerts разошёлся бы с владельцем
/// токена.
/// </para>
/// <para>
/// Отмена, а не удаление: Twitch не удаляет редемпшены, а переводит их в
/// <c>CANCELED</c>, и именно это возвращает зрителю баллы.
/// </para>
/// </remarks>
public sealed class RewardRefundConsumer(
    IOptions<RabbitMqOptions> options,
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<RewardRefundConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "twitchcore",
        QueueName,
        [RabbitMqConfig.RewardRefund],
        logger
    )
{
    public const string QueueName = "twitchcore.rewardrefund";

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var request = Deserialize<RewardRefundEvent>(json);

        if (request is null)
        {
            logger.LogWarning(
                "Reward refund on {RoutingKey} could not be deserialized",
                routingKey
            );
        }
        else if (
            string.IsNullOrWhiteSpace(request.RewardId)
            || string.IsNullOrWhiteSpace(request.RedemptionId)
        )
        {
            logger.LogWarning(
                "Reward refund for {UserName} has no reward or redemption id, skipped",
                request.UserName
            );
        }
        else
        {
            var accessToken = tokenService.Token?.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                logger.LogWarning(
                    "Reward refund for {UserName} skipped: Twitch token is missing",
                    request.UserName
                );
            }
            else
            {
                await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                    TwitchConstants.ChannelId,
                    request.RewardId,
                    [request.RedemptionId],
                    new UpdateCustomRewardRedemptionStatusRequest
                    {
                        Status = CustomRewardRedemptionStatus.CANCELED,
                    },
                    accessToken
                );

                logger.LogInformation(
                    "Reward {RewardId} refunded to {UserName}: {Reason}",
                    request.RewardId,
                    request.UserName,
                    request.Reason
                );
            }
        }
    }
}
