using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Регистрация серверной части активаций наград.
/// </summary>
/// <remarks>
/// <para>
/// Эффекты перечислены явно, а не найдены рефлексией, как прежние
/// <c>IRewardAlertHandler</c>: ключ эффекта — часть контракта наград, и
/// регистрация через отражение сделала бы «эффект забыт» неотличимым от
/// «награда без эффекта». У двух наград (Fumo Friday Night и Miku Monday Alert)
/// один класс эффекта с разными правилами, и рефлексия их не различила бы
/// вовсе.
/// </para>
/// <para>
/// <see cref="MikuMikuBeamEffect"/> — ещё и <see cref="IChatUserTrackingHandler"/>,
/// поэтому регистрируется конкретным типом, а оба интерфейса получают один и тот
/// же экземпляр: два объекта означали бы два разных окна участников чата.
/// </para>
/// </remarks>
public static class RewardActivationRegistration
{
    public static IServiceCollection AddMarsRewardActivation(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<PerStreamRewardLimiter>();
        services.TryAddSingleton<IRewardChatNotifier, RewardChatNotifier>();
        services.TryAddSingleton<IRewardRefundRequestor, RewardRefundRequestor>();
        services.TryAddSingleton<IRewardMediaResolver, RewardMediaResolver>();
        services.TryAddSingleton<IRewardRedemptionProcessor, RewardRedemptionProcessor>();
        services.TryAddSingleton<IGameCloser, GameProcessCloser>();

        services.AddSingleton<IRewardActivationEffect, ConfettiEffect>();
        services.AddSingleton<IRewardActivationEffect, FireworksEffect>();
        services.AddSingleton<IRewardActivationEffect, AdhdEffect>();
        services.AddSingleton<IRewardActivationEffect, TikTokEditEffect>();
        services.AddSingleton<IRewardActivationEffect, GaoAlertEffect>();
        services.AddSingleton<IRewardActivationEffect, CloseGameEffect>();
        services.AddSingleton<IRewardActivationEffect, AllRefundEffect>();
        services.AddSingleton<IRewardActivationEffect, RandomArtEffect>();

        services.TryAddSingleton<MikuMikuBeamEffect>();
        services.AddSingleton<IRewardActivationEffect>(sp =>
            sp.GetRequiredService<MikuMikuBeamEffect>()
        );
        services.TryAddSingleton<IChatUserTrackingHandler>(sp =>
            sp.GetRequiredService<MikuMikuBeamEffect>()
        );

        AddLimitedReward(
            services,
            new LimitedRewardRule(
                RewardAlertKeys.FumoFridayNight,
                "Fumo Friday Night",
                "Fumo Friday Night! 🧸",
                TimeSpan.FromMinutes(3)
            )
        );

        AddLimitedReward(
            services,
            new LimitedRewardRule(
                RewardAlertKeys.MikuMonday,
                "Miku Monday Alert",
                "Miku Monday Alert! 🎤",
                TimeSpan.FromMinutes(20)
            )
        );

        return services;
    }

    private static void AddLimitedReward(IServiceCollection services, LimitedRewardRule rule)
    {
        services.AddSingleton<IRewardActivationEffect>(sp =>
            ActivatorUtilities.CreateInstance<LimitedRewardEffect>(sp, rule)
        );
    }
}
