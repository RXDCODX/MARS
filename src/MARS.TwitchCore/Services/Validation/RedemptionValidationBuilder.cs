using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;

namespace MARS.TwitchCore.Services.Validation;

public sealed class RedemptionValidationBuilder(
    ChannelPointsCustomRewardRedemptionArgs args,
    ITwitchClient client,
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
                    if (
                        args.Payload?.Event?.BroadcasterUserId != TwitchConstants.ChannelId
                    )
                    {
                        throw new ValidationException(
                            "Это работает только на основном канале"
                        );
                    }

                    return Task.CompletedTask;
                },
                loud
            )
        );

        return this;
    }

    public IRedemptionValidationBuilder RequireBroadcasterUserLogin(bool loud = false)
    {
        _checks.Add(
            (
                () =>
                {
                    if (
                        !string.Equals(
                            args.Payload?.Event?.BroadcasterUserLogin,
                            TwitchConstants.Channel,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        throw new ValidationException(
                            "Это работает только на основном канале"
                        );
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
                    if (args.Payload?.Event?.Reward?.Cost != cost)
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

    public IRedemptionValidationBuilder RequireServiceActive(bool isActive, bool loud = false)
    {
        _checks.Add(
            (
                () =>
                {
                    if (!isActive)
                    {
                        throw new ValidationException("Сервис временно неактивен");
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

    public IRedemptionValidationBuilder RequireRewardGuid(Guid? expected, bool loud = false)
    {
        _checks.Add(
            (
                () =>
                {
                    if (!expected.HasValue)
                    {
                        throw new ValidationException("Награда не настроена");
                    }

                    var rewardId = args.Payload?.Event?.Reward?.Id;
                    if (
                        string.IsNullOrWhiteSpace(rewardId)
                        || !Guid.TryParse(rewardId, out var parsed)
                        || parsed != expected.Value
                    )
                    {
                        throw new ValidationException("Награда не найдена");
                    }

                    return Task.CompletedTask;
                },
                loud
            )
        );

        return this;
    }

    public async Task<ValidationResult> ValidateAsync()
    {
        var result = new ValidationResult();
        var silentFailed = false;

        foreach (var (check, loud) in _checks)
        {
            if (loud && silentFailed)
            {
                continue;
            }

            try
            {
                await check();
            }
            catch (ValidationException ex)
            {
                if (loud)
                {
                    result.AddError(ex.Message);
                }
                else
                {
                    silentFailed = true;
                    result.HasSilentFailure = true;
                }
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateWithResponseAsync(string userName)
    {
        var result = await ValidateAsync();

        if (result is { IsInvalid: true, FirstError: not null })
        {
            try
            {
                await client.SendMessageAsync(
                    TwitchConstants.Channel,
                    $"@{userName}, {result.FirstError}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send redemption validation error message");
            }
        }

        return result;
    }
}
