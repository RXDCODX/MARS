using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда с лимитами: одна активация на зрителя за стрим и общий кулдаун.
/// </summary>
/// <remarks>
/// <para>
/// Повторная активация зрителем возвращает баллы (<c>UpdateRedemptionStatus</c>
/// через MARS.TwitchCore) и пишет об этом в чат. Общий кулдаун баллы не
/// возвращает: зритель потратил их на награду, которая сейчас недоступна, но
/// это его собственный клик, а не повтор — так было в монолите, и менять это
/// поведение в переносе нельзя.
/// </para>
/// <para>
/// Один класс на две награды (Fumo Friday Night и Miku Monday Alert): они
/// отличаются только ключом, названием и длиной кулдауна, а вся логика лимита
/// общая. Две копии разошлись бы на первой правке текста.
/// </para>
/// </remarks>
public sealed class LimitedRewardEffect(
    LimitedRewardRule rule,
    PerStreamRewardLimiter limiter,
    IRewardChatNotifier chat,
    IRewardRefundRequestor refund,
    ILogger<LimitedRewardEffect> logger
) : IRewardActivationEffect
{
    public string AlertKey => rule.AlertKey;

    public async Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        var result = RewardEffectResult.Suppressed;
        var decision = limiter.TryActivate(rule.AlertKey, context.UserId, rule.GlobalCooldown);

        if (decision.IsAllowed)
        {
            await chat.SendAsync(
                $"@{context.UserName} активировал {rule.ActivatedText}",
                context.UserId,
                cancellationToken
            );

            logger.LogInformation(
                "{Reward} activated by {UserName} for {Cost} points",
                rule.DisplayName,
                context.UserName,
                context.Cost
            );

            result = new RewardEffectResult
            {
                Data = new
                {
                    DisplayName = context.UserName,
                    Color = context.Reward.User?.ChatColor,
                },
            };
        }
        else if (decision.Denial == RewardLimitDenial.AlreadyActivatedThisStream)
        {
            await refund.RequestAsync(
                new RewardRefundEvent
                {
                    RewardId = context.Reward.RewardId,
                    RedemptionId = context.Reward.MessageId ?? string.Empty,
                    UserName = context.UserName,
                    Reason = $"{rule.DisplayName}: повторная активация в этом стриме",
                },
                cancellationToken
            );

            await chat.SendAsync(
                $"@{context.UserName}, ты уже активировал {rule.DisplayName} в этом стриме! Баллы возвращены.",
                cancellationToken: cancellationToken
            );

            logger.LogInformation(
                "{Reward}: points returned to {UserName}",
                rule.DisplayName,
                context.UserName
            );
        }
        else
        {
            await chat.SendAsync(
                $"@{context.UserName}, общий кулдаун {rule.DisplayName}! Осталось {decision.RemainingSeconds} сек.",
                cancellationToken: cancellationToken
            );

            logger.LogInformation(
                "{Reward}: global cooldown, {Seconds} seconds remaining",
                rule.DisplayName,
                decision.RemainingSeconds
            );
        }

        return result;
    }
}
