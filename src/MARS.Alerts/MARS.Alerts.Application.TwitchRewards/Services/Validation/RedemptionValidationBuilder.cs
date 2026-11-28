using Grpc.Core;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards.Services.Validation;

public sealed class RedemptionValidationBuilder(
    RewardRedeemedEvent rewardEvent,
    IMarsGrpcClient grpcClient,
    ILogger logger
) : IRedemptionValidationBuilder
{
    private readonly List<(Func<Task> check, bool loud)> _checks = [];

    public IRedemptionValidationBuilder RequireBroadcasterUserId(bool loud = false)
    {
        _checks.Add(
            (
                () =>
                {
                    if (string.IsNullOrWhiteSpace(rewardEvent.BroadcasterUserId) || !string.Equals(rewardEvent.BroadcasterUserId, TwitchConstants.ChannelId))
                    {
                        throw new ValidationException("Это работает только на основном канале");
                    }

                    return Task.CompletedTask;
                },
                loud
            )
        );

        return this;
    }

    public IRedemptionValidationBuilder RequireCost(int cost, bool loud = false)
    {
        _checks.Add(
            (
                () =>
                {
                    if (rewardEvent.Cost != cost)
                    {
                        throw new ValidationException("Неверная стоимость награды");
                    }

                    return Task.CompletedTask;
                },
                loud
            )
        );

        return this;
    }

    public IRedemptionValidationBuilder RequireRewardEnabled(
        Func<bool> isEnabled,
        bool loud = false
    )
    {
        _checks.Add(
            (
                () =>
                {
                    if (!isEnabled())
                    {
                        throw new ValidationException("Награда отключена");
                    }

                    return Task.CompletedTask;
                },
                loud
            )
        );

        return this;
    }

    public IRedemptionValidationBuilder RequireFollower(bool loud = true)
    {
        _checks.Add(
            (
                () =>
                {
                    var userId = rewardEvent.UserId;

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        throw new ValidationException("Не удалось проверить подписку");
                    }

                    if (
                        string.Equals(
                            userId,
                            rewardEvent.BroadcasterUserId,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        return Task.CompletedTask;
                    }

                    if (!rewardEvent.IsFollower)
                    {
                        throw new ValidationException(
                            "Подпишись на канал, чтобы использовать эту награду"
                        );
                    }

                    return Task.CompletedTask;
                },
                loud
            )
        );

        return this;
    }

    public async Task<ValidationResult> ValidateWithResponseAsync(string userName)
    {
        var result = new ValidationResult();

        foreach (var (check, loud) in _checks)
        {
            try
            {
                await check();
            }
            catch (ValidationException ex)
            {
                result.AddError(ex.Message);

                if (loud)
                {
                    result.HasLoudFailure = true;
                    break;
                }
            }
        }

        if (result is { IsInvalid: true, FirstError: not null, HasLoudFailure: true })
        {
            try
            {
                await grpcClient.Twitch.SendChatMessageAsync(
                    new MARS.Shared.Contracts.Twitch.Control.TwitchChatSendRequest
                    {
                        Message = $"@{userName}, {result.FirstError}",
                    }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send redemption validation error message");
            }
        }

        return result;
    }

    private sealed class ValidationException(string message) : Exception(message);
}
