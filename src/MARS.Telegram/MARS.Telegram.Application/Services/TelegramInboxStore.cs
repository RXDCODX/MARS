using MARS.Telegram.Application.Models;

namespace MARS.Telegram.Application.Services;

/// <summary>
/// Потокобезопасный кольцевой буфер недавно полученных сообщений для страницы консоли.
/// </summary>
/// <remarks>
/// Синглтон по всему сервису; пишут рекордеры из разных потоков (MTProto-обновления
/// приходят в фоновом потоке), читает REST-контроллер из HTTP-потока. Лимит
/// фиксирован: старые записи вытесняются, чтобы память не росла бесконечно.
/// </remarks>
public sealed class TelegramInboxStore
{
    /// <summary>Сколько записей держим в памяти.</summary>
    public const int Capacity = 50;

    private readonly object _lock = new();
    private readonly LinkedList<TelegramInboxEntry> _entries = new();

    /// <summary>Добавить запись (новые сверху).</summary>
    public void Add(TelegramInboxEntry entry)
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

    /// <summary>Снимок записей (новые сверху).</summary>
    public IReadOnlyList<TelegramInboxEntry> Snapshot()
    {
        lock (_lock)
        {
            return _entries.ToArray();
        }
    }
}
