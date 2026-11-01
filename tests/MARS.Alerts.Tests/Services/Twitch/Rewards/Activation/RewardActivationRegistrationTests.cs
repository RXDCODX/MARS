using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Alerts.Services.Twitch.Rewards.Activation;
using MARS.Shared.Clients;
using MARS.Shared.Matoi;
using MARS.Shared.Messaging;
using MARS.Shared.Models.Rewards;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MARS.Alerts.Tests.Services.Twitch.Rewards.Activation;

/// <summary>
/// Composition root серверной части наград.
/// </summary>
/// <remarks>
/// Регистрация перечислена руками намеренно: ключ эффекта — часть контракта
/// наград, и «эффект забыт» не должен быть неотличим от «награда без эффекта».
/// Тест держит этот список: новая награда без строки в
/// <see cref="RewardActivationRegistration"/> провалит проверку, а не молча
/// перестанет работать.
/// </remarks>
public class RewardActivationRegistrationTests
{
    private static readonly string[] ExpectedKeys =
    [
        RewardAlertKeys.Confetti,
        RewardAlertKeys.Fireworks,
        RewardAlertKeys.AdhdSuperpower,
        RewardAlertKeys.TikTokEdit,
        RewardAlertKeys.GaoAlert,
        RewardAlertKeys.CloseGame,
        RewardAlertKeys.AllRefund,
        RewardAlertKeys.RandomArt,
        RewardAlertKeys.MikuMikuBeam,
        RewardAlertKeys.FumoFridayNight,
        RewardAlertKeys.MikuMonday,
    ];

    [Fact]
    public void AllEffectsAreRegisteredOnTheirKeys()
    {
        using var provider = Build();

        var keys = provider
            .GetServices<IRewardActivationEffect>()
            .Select(effect => effect.AlertKey)
            .ToArray();

        Assert.Equal(ExpectedKeys.Order(), keys.Order());
    }

    [Fact]
    public void KeysAreUnique()
    {
        using var provider = Build();

        var keys = provider
            .GetServices<IRewardActivationEffect>()
            .Select(effect => effect.AlertKey)
            .ToArray();

        Assert.Equal(keys.Length, keys.Distinct().Count());
    }

    /// <summary>
    /// MIKU MIKU BEAM — и эффект, и трекер чата одновременно: один экземпляр на
    /// два интерфейса. Два объекта означали бы два разных окна участников.
    /// </summary>
    [Fact]
    public void MikuMikuBeamIsOneInstanceForBothInterfaces()
    {
        using var provider = Build();

        var effect = provider.GetRequiredService<MikuMikuBeamEffect>();
        var tracker = provider.GetRequiredService<IChatUserTrackingHandler>();
        var registered = provider
            .GetServices<IRewardActivationEffect>()
            .Single(candidate => candidate.AlertKey == RewardAlertKeys.MikuMikuBeam);

        Assert.Same(effect, tracker);
        Assert.Same(effect, registered);
    }

    [Fact]
    public void LimitedRewardsUseDifferentCooldowns()
    {
        using var provider = Build();

        var registry = provider.GetServices<IRewardActivationEffect>().ToArray();

        Assert.Contains(registry, effect => effect.AlertKey == RewardAlertKeys.FumoFridayNight);
        Assert.Contains(registry, effect => effect.AlertKey == RewardAlertKeys.MikuMonday);
    }

    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Mock.Of<IMatoiPostService>());
        services.AddSingleton(Mock.Of<IMarsEventBus>());
        services.AddSingleton(Mock.Of<IMediaStorageClient>());
        services.AddMarsRewardActivation();

        return services.BuildServiceProvider();
    }
}
