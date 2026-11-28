namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

/// <summary>
/// Результат разбиения сообщения на части.
/// </summary>
public readonly record struct ChunkResult(string[] Chunks, int SuppressedChars);