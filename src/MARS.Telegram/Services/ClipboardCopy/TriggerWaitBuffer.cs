using System.Threading;

namespace MARS.Telegram.Services.ClipboardCopy;

internal sealed class TriggerWaitBuffer
{
    public bool HasTrigger { get; set; }
    public CancellationTokenSource? TimeoutCts { get; set; }
}
