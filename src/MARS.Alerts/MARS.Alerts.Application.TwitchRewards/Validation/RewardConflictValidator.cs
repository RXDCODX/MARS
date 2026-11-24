using System;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;

namespace MARS.Alerts.Application.TwitchRewards.Validation;

/// <summary>
/// Обнаружение конфликтов планировочной активности среди обработчиков наград
/// с одинаковой стоимостью. Возвращает список описаний конфликтов (пустой —
/// всё в порядке). Используется как стартовая проверка и в тестах.
/// </summary>
public static class RewardConflictValidator
{
    public static IReadOnlyList<string> FindConflicts(IEnumerable<IRewardAlertHandler> handlers)
    {
        var conflicts = new List<string>();

        var grouped = handlers.GroupBy(h => h.RewardCost).Where(g => g.Count() > 1).ToList();

        foreach (var group in grouped)
        {
            var cost = group.Key;
            var handlerList = group.ToList();

            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var active = handlerList
                    .Where(h => RewardPolicyDaySets.GetSchedulingDays(h.Activation).Contains(day))
                    .Select(h => h.GetType().Name)
                    .ToList();

                if (active.Count >= 2)
                {
                    conflicts.Add($"Cost={cost} at {day}: [{string.Join(", ", active)}]");
                }
            }
        }

        return conflicts;
    }

    public static void ThrowIfConflicts(IEnumerable<IRewardAlertHandler> handlers)
    {
        var conflicts = FindConflicts(handlers);
        if (conflicts.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Конфликт активности наград с одинаковой стоимостью:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, conflicts)
        );
    }

    public static IReadOnlyList<string> FindOtherIssues(IEnumerable<IRewardAlertHandler> handlers)
    {
        var issues = new List<string>();

        foreach (var h in handlers)
        {
            if (h.RewardCost <= 0)
            {
                issues.Add($"{h.GetType().Name}: RewardCost={h.RewardCost} (must be >0)");
            }

            if (string.IsNullOrWhiteSpace(h.AlertDisplayName))
            {
                issues.Add($"{h.GetType().Name}: AlertDisplayName is empty");
            }
        }

        var duplicateTitles = handlers
            .GroupBy(h => h.AlertDisplayName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} [{string.Join(", ", g.Select(h => h.GetType().Name))}]")
            .ToList();
        issues.AddRange(duplicateTitles.Select(t => $"Duplicate AlertDisplayName: {t}"));

        return issues;
    }

    public static void ThrowIfIssues(IEnumerable<IRewardAlertHandler> handlers)
    {
        var issues = FindOtherIssues(handlers);
        var conflicts = FindConflicts(handlers);
        var all = conflicts.Concat(issues).ToList();
        if (all.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Проблемы регистрации наград:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, all)
        );
    }
}
