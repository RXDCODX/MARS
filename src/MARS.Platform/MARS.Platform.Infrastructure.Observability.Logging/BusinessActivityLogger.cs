using System.Text.RegularExpressions;
using MARS.Shared.Kernel.Observability;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Infrastructure.Observability.Logging;

/// <summary>
/// Реализация <see cref="IBusinessActivityLogger" /> на основе <see cref="ILogger{T}" />.
/// Сервис и трассировка добавляются общей настройкой Serilog.
/// </summary>
public sealed class BusinessActivityLogger : IBusinessActivityLogger
{
    private const int MaxSummaryLength = 2000;
    private const int MaxActorLength = 120;
    private const int MaxMetadataKeyLength = 64;

    private static readonly Regex ActivityNamePattern = new(
        @"^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private readonly ILogger<BusinessActivityLogger> _logger;

    public BusinessActivityLogger(ILogger<BusinessActivityLogger> logger)
    {
        _logger = logger;
    }

    public void Log(BusinessActivity activity)
    {
        Write(activity, null);
    }

    public void Log(BusinessActivity activity, Exception exception)
    {
        Write(activity, exception);
    }

    private void Write(BusinessActivity activity, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var category = NormalizeActivityName(activity.Category, "business");
        var action = NormalizeActivityName(activity.Action, "activity");
        var outcome = activity.Outcome.ToString().ToLowerInvariant();
        var actor = NormalizeActor(activity.ActorDisplayName);
        var platform = NormalizePlatform(activity.Platform);
        var summary = NormalizeSummary(activity.Summary);
        var level = MapLevel(activity.Level, activity.Outcome);

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["business_activity"] = true,
            ["activity_category"] = category,
            ["activity_action"] = action,
            ["activity_outcome"] = outcome,
            ["activity_actor_type"] = activity.ActorType,
            ["activity_actor_id"] = activity.ActorId,
            ["activity_actor_display_name"] = actor,
            ["activity_platform"] = platform,
            ["activity_subject_type"] = activity.SubjectType,
            ["activity_subject_id"] = activity.SubjectId,
            ["activity_correlation_id"] = activity.CorrelationId,
            ["activity_duration_ms"] = activity.DurationMs,
            ["activity_summary"] = summary,
        };

        if (activity.Metadata is not null)
        {
            foreach (var (key, value) in activity.Metadata)
            {
                if (string.IsNullOrWhiteSpace(key) || key.Length > MaxMetadataKeyLength)
                {
                    continue;
                }

                properties[$"activity_meta_{key}"] = value;
            }
        }

        using var scope = _logger.BeginScope(properties);

        var eventId = new EventId(0, action);

        if (exception is null)
        {
            _logger.Log(level, eventId, "[{ActorDisplayName}] {Summary:lj}", actor, summary);
        }
        else
        {
            _logger.Log(
                level,
                eventId,
                exception,
                "[{ActorDisplayName}] {Summary:lj}",
                actor,
                summary
            );
        }
    }

    private static string NormalizeActivityName(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim().ToLowerInvariant().Replace(' ', '-');

        return ActivityNamePattern.IsMatch(normalized) ? normalized : fallback;
    }

    private static string NormalizeActor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "system";
        }

        var normalized = value.Trim().Replace("\r", " ").Replace("\n", " ");

        if (normalized.Length > MaxActorLength)
        {
            normalized = normalized[..MaxActorLength] + "…";
        }

        return normalized.Replace("[", "(").Replace("]", ")");
    }

    private static string? NormalizePlatform(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace("\r", " ").Replace("\n", " ");

        return normalized.Length <= 64 ? normalized : normalized[..64];
    }

    private static string NormalizeSummary(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "без описания";
        }

        var normalized = value.Trim().Replace("\r", " ").Replace("\n", " ");

        if (normalized.Length > MaxSummaryLength)
        {
            normalized = normalized[..MaxSummaryLength] + "…";
        }

        return normalized;
    }

    private static LogLevel MapLevel(BusinessActivityLevel level, BusinessActivityOutcome outcome)
    {
        return level switch
        {
            BusinessActivityLevel.Warning => LogLevel.Warning,
            BusinessActivityLevel.Error => LogLevel.Error,
            _ => outcome == BusinessActivityOutcome.Failed ? LogLevel.Error : LogLevel.Information,
        };
    }
}
