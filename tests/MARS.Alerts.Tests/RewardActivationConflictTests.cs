using System;
using System.Drawing;
using System.Linq;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.DependencyInjection;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Validation;
using MARS.TestKit;
using Xunit;

namespace MARS.Alerts.Tests;

public class RewardActivationConflictTests
{
    private static IReadOnlyList<IRewardAlertHandler> RealHandlers() =>
        [
            .. RewardHandlerRegistration
                .DiscoverHandlerTypes()
                .Select(type => (IRewardAlertHandler)Stub.Resolve(type, depth: 3)!),
        ];

    [Fact]
    public void AllSharedCostGroups_HaveNoOverlappingSchedulingDays()
    {
        var handlers = RealHandlers();
        var conflicts = RewardConflictValidator.FindConflicts(handlers);

        Assert.True(
            conflicts.Count == 0,
            "Конфликт активности наград с одинаковой стоимостью:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, conflicts)
        );
    }

    [Fact]
    public void Cost4_Group_CoversAllDaysOfWeek_WithoutGaps()
    {
        var handlers = RealHandlers();
        var cost4 = handlers.Where(h => h.RewardCost == 4).ToList();

        Assert.True(cost4.Count >= 4, $"Ожидалось ≥4 наград с Cost=4, получено {cost4.Count}");

        var gaps = new List<DayOfWeek>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var active = cost4.Count(h =>
                RewardPolicyDaySets.GetSchedulingDays(h.Activation).Contains(day)
            );
            Assert.True(
                active <= 1,
                $"Cost=4: {active} наград активны в {day}: "
                    + string.Join(
                        ", ",
                        cost4
                            .Where(h =>
                                RewardPolicyDaySets.GetSchedulingDays(h.Activation).Contains(day)
                            )
                            .Select(h => h.GetType().Name)
                    )
            );
            if (active == 0)
            {
                gaps.Add(day);
            }
        }

        Assert.True(gaps.Count == 0, $"Cost=4: нет активной награды в [{string.Join(", ", gaps)}]");
    }

    [Fact]
    public void AllHandlers_HavePositiveCost()
    {
        var handlers = RealHandlers();
        var bad = handlers.Where(h => h.RewardCost <= 0).Select(h => h.GetType().Name).ToList();
        Assert.True(bad.Count == 0, "Неположительный Cost: " + string.Join(", ", bad));
    }

    [Fact]
    public void AllHandlers_HaveNonEmptyUniqueDisplayNames()
    {
        var handlers = RealHandlers();
        var empty = handlers
            .Where(h => string.IsNullOrWhiteSpace(h.AlertDisplayName))
            .Select(h => h.GetType().Name)
            .ToList();
        Assert.True(empty.Count == 0, "Пустой AlertDisplayName: " + string.Join(", ", empty));

        var dupes = handlers
            .GroupBy(h => h.AlertDisplayName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} [{string.Join(", ", g.Select(h => h.GetType().Name))}]")
            .ToList();
        Assert.True(dupes.Count == 0, "Дубликаты AlertDisplayName: " + string.Join("; ", dupes));
    }

    [Fact]
    public void ThrowIfIssues_ThrowsOnSyntheticConflict()
    {
        var a = new ConflictStub(4, AlwaysActiveRewardPolicy.Instance, "stub-A");
        var b = new ConflictStub(4, AlwaysActiveRewardPolicy.Instance, "stub-B");

        Assert.Throws<InvalidOperationException>(() =>
            RewardConflictValidator.ThrowIfIssues(new[] { a, b })
        );
    }

    [Fact]
    public void ThrowIfIssues_PassesForNonOverlappingGroup()
    {
        var monday = new DayOfWeekPolicy(DayOfWeek.Monday);
        var notMonWedFri = new NotDaysPolicy([
            DayOfWeek.Monday,
            DayOfWeek.Wednesday,
            DayOfWeek.Friday,
        ]);

        var a = new ConflictStub(4, monday, "stub-A");
        var b = new ConflictStub(4, notMonWedFri, "stub-B");

        RewardConflictValidator.ThrowIfIssues(new[] { a, b }); // no throw
    }

    [Fact]
    public void FindConflicts_ReportsOverlappingDay()
    {
        var a = new ConflictStub(4, new DayOfWeekPolicy(DayOfWeek.Monday), "stub-A");
        var b = new ConflictStub(4, new DayOfWeekPolicy(DayOfWeek.Monday), "stub-B");

        var conflicts = RewardConflictValidator.FindConflicts(new[] { a, b });

        Assert.Single(conflicts);
        Assert.Contains("Cost=4", conflicts[0]);
        Assert.Contains("Monday", conflicts[0]);
    }

    private sealed class ConflictStub : RewardHandlerBase
    {
        public ConflictStub(int cost, IRewardActivationPolicy activation, string? name = null)
        {
            Cost = cost;
            RewardCostValue = cost;
            ActivationValue = activation;
            AlertDisplayName = name ?? ("stub-" + cost);
        }

        public int RewardCostValue { get; }
        public override int RewardCost => RewardCostValue;
        public override string AlertDisplayName { get; set; }
        public override string AlertDescription { get; set; } = string.Empty;
        public override Color Color { get; set; } = Color.White;
        public override int Cost { get; init; }
        public IRewardActivationPolicy ActivationValue { get; }
        public override IRewardActivationPolicy Activation => ActivationValue;

        public override Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
