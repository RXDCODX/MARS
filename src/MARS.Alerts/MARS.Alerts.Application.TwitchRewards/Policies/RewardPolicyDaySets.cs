using System;

namespace MARS.Alerts.Application.TwitchRewards.Policies;

/// <summary>
/// Символьный анализ планировочной активности политики по дням недели.
/// </summary>
public static class RewardPolicyDaySets
{
    public static IReadOnlySet<DayOfWeek> GetSchedulingDays(IRewardActivationPolicy policy)
    {
        if (policy.IsStateful)
        {
            return AllDays;
        }

        return policy switch
        {
            DayOfWeekPolicy p => new HashSet<DayOfWeek> { p.Day },
            NotDaysPolicy p => AllDays.Except(p.Days).ToHashSet(),
            AlwaysActiveRewardPolicy => new HashSet<DayOfWeek>(AllDays),
            DisabledPolicy => new HashSet<DayOfWeek>(),
            ServiceActivePolicy => new HashSet<DayOfWeek>(AllDays),
            CompositeRewardPolicy c => c
                .Policies.Where(p => !p.IsStateful)
                .Select(GetSchedulingDays)
                .DefaultIfEmpty(AllDays)
                .Aggregate((a, b) => a.Intersect(b).ToHashSet()),
            _ => new HashSet<DayOfWeek>(AllDays),
        };
    }

    public static readonly IReadOnlySet<DayOfWeek> AllDays = new HashSet<DayOfWeek>(
        Enum.GetValues<DayOfWeek>()
    );
}
