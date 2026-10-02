using System.Drawing;
using System.Reflection;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.ChannelRewards;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Временная награда канала.
///
/// Награда создаётся на старте и держится в актуальном состоянии по таймеру.
/// Проверяется решение о состоянии с учётом выопределения из конфигурации: награда,
/// которую администратор выключил в настройках, не должна включаться обратно флагом,
/// иначе зрители видели бы отключённую награду как рабочую.
/// </summary>
public class TemporaryRewardTests
{
    [Fact]
    public async Task InactiveRewardIsNotCreated()
    {
        var rewards = new FakeRewards();

        await StartAsync(new StubReward(rewards, enabled: () => true, isActive: false));

        Assert.Empty(rewards.Created);
    }

    /// <summary>
    /// Запрос на создание собирается из полей награды: пустое название или стоимость
    /// Twitch отверг бы, и зритель не увидел бы награду вообще.
    /// </summary>
    [Fact]
    public async Task RequestIsBuiltFromRewardFields()
    {
        var rewards = new FakeRewards();

        await StartAsync(new StubReward(rewards, enabled: () => true, cost: 500));

        var request = Assert.Single(rewards.Created);
        Assert.Equal(500, request.Cost);
        Assert.Equal("Тестовая награда", request.Title);
        Assert.Equal("Описание тестовой награды", request.Prompt);
        Assert.True(request.IsEnabled);
    }

    /// <summary>
    /// Цвет награды переводится в HEX с решёткой, как того ждёт Twitch.
    /// </summary>
    [Fact]
    public async Task ColorIsSentAsHex()
    {
        var rewards = new FakeRewards();

        await StartAsync(
            new StubReward(rewards, enabled: () => true, color: Color.FromArgb(255, 18, 52, 86))
        );

        Assert.Equal("#123456", Assert.Single(rewards.Created).BackgroundColor);
    }

    /// <summary>
    /// Переопределение из конфигурации важнее собственного флага награды.
    /// </summary>
    [Fact]
    public async Task ConfigOverrideDisablesReward()
    {
        var rewards = new FakeRewards { Override = false };

        await StartAsync(new StubReward(rewards, enabled: () => true, cost: 100));

        Assert.False(Assert.Single(rewards.Created).IsEnabled);
    }

    /// <summary>
    /// Без переопределения действует собственный флаг награды.
    /// </summary>
    [Fact]
    public async Task OwnFlagEnablesReward()
    {
        var rewards = new FakeRewards();

        await StartAsync(new StubReward(rewards, enabled: () => true, cost: 100));

        Assert.True(Assert.Single(rewards.Created).IsEnabled);
    }

    /// <summary>
    /// Выключенная награда создаётся выключенной: подписчик наград нужен, чтобы
    /// её можно было включить без перезапуска, но показываться зрителю она не
    /// должна.
    /// </summary>
    [Fact]
    public async Task DisabledRewardIsCreatedButOff()
    {
        var rewards = new FakeRewards();

        await StartAsync(new StubReward(rewards, enabled: () => false));

        Assert.False(Assert.Single(rewards.Created).IsEnabled);
    }

    /// <summary>
    /// Уже существующая награда обновляется, а не создаётся заново: иначе на канале
    /// появлялись бы дубли одной и той же награды.
    /// </summary>
    [Fact]
    public async Task ExistingRewardIsUpdated()
    {
        var rewards = new FakeRewards();
        rewards.Existing.Add(ExistingReward(isEnabled: false));

        var reward = new StubReward(rewards, enabled: () => true);
        await StartAsync(reward);

        Assert.Empty(rewards.Created);
        var update = Assert.Single(rewards.Updated);
        Assert.Equal(FakeRewards.RewardId, update.RewardId);
        Assert.True(update.Request.IsEnabled == true);
    }

    /// <summary>
    /// Ручной перезапуск по таймеру пересчитывает состояние тем же правилом: иначе
    /// выключенная награда включалась бы обратно до следующего перезапуска.
    /// </summary>
    /// <summary>
    /// Ручной перезапуск по таймеру пересчитывает состояние: после смены
    /// переопределения в конфигурации награда включается без перезапуска сервиса.
    /// </summary>
    [Fact]
    public async Task ManualTickAppliesConfigOverride()
    {
        var rewards = new FakeRewards { Override = false };
        rewards.Existing.Add(ExistingReward(isEnabled: true));
        var reward = new StubReward(rewards, enabled: () => true, cost: 100);

        await StartAsync(reward);
        Assert.False(rewards.Updated[^1].Request.IsEnabled == true);

        rewards.Override = true;
        TimerElapseNow(reward);
        await WaitUntilAsync(() => rewards.Updated.Count >= 2);

        Assert.True(rewards.Updated[^1].Request.IsEnabled == true);
    }

    /// <summary>
    /// Ошибка обновления в фоновом цикле не роняет сервис: она ловится, награда
    /// остаётся как была, а остановка работает.
    /// </summary>
    [Fact]
    public async Task UnavailableTwitchDoesNotBreakCycle()
    {
        var rewards = new FakeRewards { Override = false };
        rewards.Existing.Add(ExistingReward(isEnabled: true));
        var reward = new StubReward(rewards, enabled: () => true);

        await reward.StartAsync(Token);

        rewards.ThrowOnUpdateNumber = rewards.UpdateAttempts + 1;
        rewards.Override = true;
        TimerElapseNow(reward);
        await WaitUntilAsync(() => rewards.UpdateAttempts >= 2);

        await reward.StopAsync(Token);
    }

    /// <summary>
    /// На старте ошибка Twitch не скрывается: награда обязана быть готова до начала
    /// трансляции, иначе зритель не увидит её вовсе.
    /// </summary>
    [Fact]
    public async Task StartupFailureIsReported()
    {
        var rewards = new FakeRewards { Override = false, ThrowOnUpdateNumber = 1 };
        rewards.Existing.Add(ExistingReward(isEnabled: true));
        var reward = new StubReward(rewards, enabled: () => true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => reward.StartAsync(Token));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>
    /// Фоновые вызовы выполняются без ожидания, поэтому результат дожидается по
    /// счётчику попыток, а не по времени сна.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20, Token);
        }

        Assert.Fail("Фоновое обновление награды не выполнилось");
    }

    private static async Task StartAsync(TemporaryReward reward)
    {
        await reward.StartAsync(Token);
        await reward.StopAsync(Token);
    }

    private static void TimerElapseNow(TemporaryReward reward)
    {
        var method = typeof(TemporaryReward).GetMethod(
            "TimerElapseNow",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        method.Invoke(reward, []);
    }

    /// <summary>
    /// Уже существующая награда в состоянии Twitch. Модели TwitchLib собираются
    /// рефлексией: их сеттеры помечены <c>internal</c>.
    /// </summary>
    private static CustomReward ExistingReward(bool isEnabled) =>
        TwitchLibModels.Create<CustomReward>(
            ("Id", FakeRewards.RewardId),
            ("Title", "Тестовая награда"),
            ("Prompt", "Описание тестовой награды"),
            ("Cost", 100),
            ("IsEnabled", isEnabled),
            ("IsUserInputRequired", false),
            ("BackgroundColor", "#010203"),
            ("ShouldRedemptionsSkipQueue", false),
            (
                "GlobalCooldownSetting",
                TwitchLibModels.Create<GlobalCooldownSetting>(
                    ("IsEnabled", false),
                    ("GlobalCooldownSeconds", 0)
                )
            ),
            (
                "MaxPerStreamSetting",
                TwitchLibModels.Create<MaxPerStreamSetting>(("IsEnabled", false))
            ),
            (
                "MaxPerUserPerStreamSetting",
                TwitchLibModels.Create<MaxPerUserPerStreamSetting>(("IsEnabled", false))
            )
        );

    /// <summary>
    /// Конкретная награда для теста: абстрактный класс задаёт контракт, а нужны
    /// конкретные название, стоимость и флаг.
    /// </summary>
    private sealed class StubReward(
        IChannelRewardsService rewards,
        Func<bool> enabled,
        bool isActive = true,
        int cost = 100,
        Color? color = null
    ) : TemporaryReward(rewards, NullLogger.Instance)
    {
        public override string AlertDisplayName { get; set; } = "Тестовая награда";

        public override string AlertDescription { get; set; } = "Описание тестовой награды";

        public override Color Color { get; set; } = color ?? Color.FromArgb(255, 1, 2, 3);

        public override int Cost { get; init; } = cost;

        public override Func<bool> IsRewardEnabled { get; set; } = enabled;

        protected override bool IsRewardActive { get; } = isActive;
    }

    /// <summary>
    /// Награды канала в памяти: <see cref="ChannelRewardsService"/> уходит в
    /// конкретный <c>Helix</c> и подменить его нечем, поэтому проверяется решение
    /// о состоянии награды, а не обмен с Twitch.
    /// </summary>
    private sealed class FakeRewards : IChannelRewardsService
    {
        public List<CustomReward> Existing { get; } = [];

        public List<CreateCustomRewardsRequest> Created { get; } = [];

        public List<(string RewardId, UpdateCustomRewardRequest Request)> Updated { get; } = [];

        public bool? Override { get; set; }

        public bool FailUpdates { get; init; }

        /// <summary>Номер попытки обновления, на которой Twitch падает.</summary>
        public int? ThrowOnUpdateNumber { get; set; }

        /// <summary>Сколько раз сервис пытался обновить награду.</summary>
        public int UpdateAttempts { get; private set; }

        /// <summary>Идентификатор награды: сервис разбирает его как GUID.</summary>
        public static string RewardId { get; } = Guid.NewGuid().ToString();

        public Mock<IRewardsCacheService> Cache { get; } = new();

        public IRewardsCacheService RewardsCacheService => Cache.Object;

        public bool? GetEnabledOverrideForCost(int cost) => Override;

        public Task<string?> CreateRewardAsync(CreateCustomRewardsRequest request)
        {
            Created.Add(request);

            return Task.FromResult<string?>(RewardId);
        }

        public Task<bool> UpdateRewardAsync(string rewardId, UpdateCustomRewardRequest request)
        {
            Updated.Add((rewardId, request));
            UpdateAttempts++;

            if (ThrowOnUpdateNumber == UpdateAttempts)
            {
                throw new InvalidOperationException("Twitch недоступен");
            }

            // Состояние переносится в запись, как это делает Twitch: иначе
            // следующая сверка увидела бы награду в старом состоянии.
            var stored = Existing.FirstOrDefault(reward => reward.Id == rewardId);
            typeof(CustomReward)
                .GetProperty(nameof(CustomReward.IsEnabled))!
                .SetValue(stored, request.IsEnabled);

            return Task.FromResult(!FailUpdates);
        }

        public Task<IEnumerable<CustomReward>?> GetRewardsAsync() =>
            Task.FromResult<IEnumerable<CustomReward>?>(Existing);
    }
}
