using System.Text.Json;
using System.Text.RegularExpressions;
using MARS.Admin.Application.Services.Logging;
using MARS.Admin.Application.Services.ServiceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MARS.Admin.Api.Services;

/// <summary>
/// Чтение журналов из Loki. Сервисный ключ не нужен: Loki доступен внутри сети.
/// </summary>
public sealed class LokiLogQueryService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<LokiLogQueryService> logger
) : ILogQueryService
{
    private static readonly Regex ActorPrefixPattern = new(
        @"^\[(?<actor>[^\]]+)\]\s*(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public async Task<IReadOnlyList<ServiceLog>> QueryAsync(
        ServiceLogQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var hours = Math.Clamp(query.Hours <= 0 ? 1 : query.Hours, 1, 168);
        var limit = Math.Clamp(query.Limit <= 0 ? 100 : query.Limit, 1, 500);
        var end = DateTimeOffset.UtcNow;
        var start = end.AddHours(-hours);

        var selector = BuildSelector(query);
        var logQuery = selector;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            logQuery += $" |= \"{EscapeLogQuery(query.Search)}\"";
        }

        var url =
            $"{GetBaseUrl()}/loki/api/v1/query_range"
            + $"?query={Uri.EscapeDataString(logQuery)}"
            + $"&start={start.ToUnixTimeMilliseconds() * 1_000_000}"
            + $"&end={end.ToUnixTimeMilliseconds() * 1_000_000}"
            + $"&limit={limit}"
            + "&direction=backward";

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Loki вернул {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    body
                );
                throw new HttpRequestException(
                    $"Loki недоступен: HTTP {(int)response.StatusCode}."
                );
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken
            );

            return Parse(document.RootElement);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Не удалось прочитать журнал из Loki.");
            throw new HttpRequestException("Не удалось прочитать журнал из Loki.", ex);
        }
    }

    private string BuildSelector(ServiceLogQuery query)
    {
        var labels = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Service))
        {
            labels.Add($"container=\"{EscapeLabel(query.Service)}\"");
        }

        if (query.BusinessOnly)
        {
            labels.Add("activity_action!=\"\"");
        }

        if (!string.IsNullOrWhiteSpace(query.ActivityType))
        {
            labels.Add($"activity_action=\"{EscapeLabel(query.ActivityType)}\"");
        }

        if (!string.IsNullOrWhiteSpace(query.Platform))
        {
            labels.Add($"activity_platform=\"{EscapeLabel(query.Platform)}\"");
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            labels.Add($"activity_outcome=\"{EscapeLabel(query.Outcome)}\"");
        }

        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            labels.Add($"level=\"{EscapeLabel(NormalizeLevel(query.Level))}\"");
        }

        if (labels.Count == 0)
        {
            labels.Add("container=~\".+\"");
        }

        return "{" + string.Join(",", labels) + "}";
    }

    private static string NormalizeLevel(string level)
    {
        return level.Trim() switch
        {
            "debug" or "Debug" => "Debug",
            "info" or "Info" or "information" or "Information" => "Information",
            "warning" or "Warning" or "warn" or "Warn" => "Warning",
            "error" or "Error" => "Error",
            "fatal" or "Fatal" => "Fatal",
            _ => level.Trim(),
        };
    }

    private static string EscapeLabel(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string EscapeLogQuery(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private string GetBaseUrl()
    {
        var configured = configuration["Loki:Url"] ?? configuration["Loki:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var port = Environment.GetEnvironmentVariable("MARS_PORT_LOKI");
        if (!string.IsNullOrWhiteSpace(port))
        {
            return $"http://localhost:{port.Trim()}";
        }

        return "http://loki:3100";
    }

    private static IReadOnlyList<ServiceLog> Parse(JsonElement root)
    {
        var result = new List<ServiceLog>();

        if (
            !root.TryGetProperty("data", out var data)
            || !data.TryGetProperty("result", out var streams)
            || streams.ValueKind != JsonValueKind.Array
        )
        {
            return result;
        }

        foreach (var stream in streams.EnumerateArray())
        {
            var labels = stream.TryGetProperty("stream", out var streamLabels)
                ? streamLabels
                : default;

            var container = GetLabel(labels, "container");
            var serviceName = GetLabel(labels, "service.name");
            var level = GetLabel(labels, "level");
            var activityType = GetLabel(labels, "activity_action");
            var platform = GetLabel(labels, "activity_platform");
            var actor = GetLabel(labels, "activity_actor_display_name");

            if (
                !stream.TryGetProperty("values", out var values)
                || values.ValueKind != JsonValueKind.Array
            )
            {
                continue;
            }

            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < 2)
                {
                    continue;
                }

                var timestampText = value[0].GetString();
                var line = value[1].GetString() ?? string.Empty;

                var timestamp = ParseTimestamp(timestampText);
                var parsed = ActorPrefixPattern.Match(line);

                var message = parsed.Success ? parsed.Groups["message"].Value : line;
                var parsedActor = parsed.Success ? parsed.Groups["actor"].Value : null;

                result.Add(
                    new ServiceLog
                    {
                        Timestamp = timestamp,
                        Level = string.IsNullOrWhiteSpace(level) ? "Information" : level,
                        Service = string.IsNullOrWhiteSpace(serviceName) ? container : serviceName,
                        Message = message,
                        RawMessage = line,
                        ActivityType = string.IsNullOrWhiteSpace(activityType)
                            ? null
                            : activityType,
                        ActorDisplayName = actor ?? parsedActor,
                        Platform = platform,
                    }
                );
            }
        }

        return result.OrderByDescending(item => item.Timestamp).ToList();
    }

    private static DateTime ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        var digits = value.Length >= 13 ? value[..13] : value.PadRight(13, '0');

        if (long.TryParse(digits, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }

        return DateTime.UtcNow;
    }

    private static string GetLabel(JsonElement labels, string name)
    {
        if (labels.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (labels.TryGetProperty(name, out var value))
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
