using System.Collections.Concurrent;
using MARS.Shared.Models;

namespace MARS.TTS.Services;

public class TtsMessageFilterService(
    ILogger<TtsMessageFilterService>? logger = null,
    TimeSpan? dedupWindow = null
) : ITtsMessageFilterService
{
    private readonly TimeSpan _dedupWindow = dedupWindow ?? TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, DateTime> _recentMessages = new(
        StringComparer.OrdinalIgnoreCase
    );

    private const int CleanupThreshold = 100;
    private int _messagesSinceCleanup;

    public bool IsFilterEnabled { get; set; } = true;

    public Task LoadStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public OperationResult<string> FilterMessage(string message, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return OperationResult<string>.Fail("Сообщение пустое");
        }

        if (!IsFilterEnabled)
        {
            return OperationResult<string>.Ok(message);
        }

        var collapsed = CollapseRepetitions(message);

        var normalized = collapsed.Trim().ToLowerInvariant();
        var now = DateTime.Now;

        if (_messagesSinceCleanup >= CleanupThreshold)
        {
            Cleanup(now);
        }

        if (
            _recentMessages.TryGetValue(normalized, out var firstSeen)
            && now - firstSeen < _dedupWindow
        )
        {
            logger?.LogDebug(
                "Сообщение от {UserId} отброшено: повтор за {Window}",
                userId ?? "аноним",
                _dedupWindow
            );

            return OperationResult<string>.Fail("Обнаружен дубликат сообщения");
        }

        _recentMessages[normalized] = now;
        _messagesSinceCleanup++;

        return OperationResult<string>.Ok(collapsed);
    }

    private void Cleanup(DateTime now)
    {
        var cutoff = now - _dedupWindow;

        foreach (var kvp in _recentMessages)
        {
            if (kvp.Value < cutoff)
            {
                _recentMessages.TryRemove(kvp.Key, out _);
            }
        }

        _messagesSinceCleanup = 0;
    }

    internal static string CollapseRepetitions(string message)
    {
        var words = message.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (words.Length < 2)
        {
            return message;
        }

        var afterWordCollapse = CollapseWordRepetitions(words);

        var result = CollapsePhraseRepetitions(afterWordCollapse);

        return result;
    }

    private static List<string> CollapseWordRepetitions(string[] words)
    {
        var result = new List<string>(words.Length);

        var i = 0;
        while (i < words.Length)
        {
            var current = words[i];
            var count = 1;

            while (
                i + count < words.Length
                && string.Equals(words[i + count], current, StringComparison.OrdinalIgnoreCase)
            )
            {
                count++;
            }

            if (count >= 3)
            {
                result.Add(current);
            }
            else
            {
                for (var j = 0; j < count; j++)
                {
                    result.Add(current);
                }
            }

            i += count;
        }

        return result;
    }

    private static string CollapsePhraseRepetitions(List<string> words)
    {
        if (words.Count < 2)
        {
            return string.Join(' ', words);
        }

        var n = words.Count;

        for (var repeats = n / 2; repeats >= 2; repeats--)
        {
            if (n % repeats != 0)
            {
                continue;
            }

            var phraseLen = n / repeats;

            var isRepeated = true;
            for (var i = phraseLen; i < n; i++)
            {
                if (
                    !string.Equals(
                        words[i],
                        words[i % phraseLen],
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    isRepeated = false;
                    break;
                }
            }

            if (isRepeated)
            {
                return string.Join(' ', words.Take(phraseLen));
            }
        }

        return string.Join(' ', words);
    }
}
