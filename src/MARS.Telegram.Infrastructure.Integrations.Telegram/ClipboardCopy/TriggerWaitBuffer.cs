using System.Threading;

namespace MARS.Telegram.Infrastructure.Integrations.Telegram.ClipboardCopy;

internal sealed class TriggerWaitBuffer
{
    public bool HasTrigger { get; set; }
    public CancellationTokenSource? TimeoutCts { get; set; }
}
