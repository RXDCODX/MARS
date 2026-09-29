using System.Collections.Concurrent;
using System.Threading;
using Telegram.Bot.Types;

namespace MARS.Telegram.Services.ClipboardCopy;

internal sealed class MediaGroupBuffer
{
    public ConcurrentDictionary<int, Message> Messages { get; } = new();
    public CancellationTokenSource? DebounceCts { get; set; }
    public int IsProcessed;

    public void ResetDebounce()
    {
        DebounceCts?.Cancel();
        DebounceCts?.Dispose();
    }
}
