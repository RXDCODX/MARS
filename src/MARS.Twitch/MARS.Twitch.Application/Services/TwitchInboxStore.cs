using MARS.Twitch.Application.Models;

namespace MARS.Twitch.Application.Services;

/// <summary>
/// Потокобезопасный кольцевой буфер недавно полученных чат-сообщений.
/// </summary>
public sealed class TwitchInboxStore
{
    public const int Capacity = 150;

    private readonly object _lock = new();
    private readonly LinkedList<TwitchInboxEntry> _entries = new();

    public void Add(TwitchInboxEntry entry)
    {
        lock (_lock)
        {
            _entries.AddFirst(entry);

            while (_entries.Count > Capacity)
            {
                _entries.RemoveLast();
            }
        }
    }

    public IReadOnlyList<TwitchInboxEntry> Snapshot(string? channel = null)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return _entries.ToArray();
            }

            var normalized = channel.Trim().TrimStart('@').ToLowerInvariant();

            return _entries
                .Where(entry =>
                    string.Equals(entry.Channel, normalized, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();
        }
    }
}
