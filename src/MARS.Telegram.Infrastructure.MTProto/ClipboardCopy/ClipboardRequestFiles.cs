using System;

namespace MARS.Telegram.Infrastructure.MTProto.ClipboardCopy;

internal sealed class ClipboardRequestFiles(string[] memoryFileNames, DateTime createdAt)
{
    public string[] MemoryFileNames { get; } = memoryFileNames;
    public DateTime CreatedAt { get; } = createdAt;
}
