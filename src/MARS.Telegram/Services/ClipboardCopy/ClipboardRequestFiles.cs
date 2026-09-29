using System;

namespace MARS.Telegram.Services.ClipboardCopy;

internal sealed class ClipboardRequestFiles(string[] memoryFileNames, DateTime createdAt)
{
    public string[] MemoryFileNames { get; } = memoryFileNames;
    public DateTime CreatedAt { get; } = createdAt;
}
