namespace MARS.TwitchCore.Services.HelloVideos;

/// <summary>
/// Ограниченный по размеру кэш недавно обработанных сообщений.
/// Аудит №17: прежний <c>List&lt;string&gt; _users</c> только рос и никогда не
/// очищался, а проверка через <c>Contains</c> была линейной — на долгоживущем
/// канале это утечка памяти и O(n) на каждое сообщение.
/// </summary>
public sealed class RecentMessageTracker(int capacity = 2000)
{
    private readonly int _capacity = capacity > 0 ? capacity : 1;
    private readonly HashSet<string> _entries = new(StringComparer.Ordinal);
    private readonly Queue<string> _order = new();

    public int Count => _entries.Count;

    /// <summary>
    /// Возвращает true, если идентификатор уже присутствовал, иначе добавляет его.
    /// При переполнении вытесняется самый старый элемент (FIFO), а не только что
    /// добавленный — иначе новые элементы вытесняли бы сами себя.
    /// </summary>
    public bool TryMarkSeen(string id)
    {
        bool result;

        lock (_entries)
        {
            result = !_entries.Add(id);

            if (!result)
            {
                _order.Enqueue(id);

                while (_order.Count > _capacity)
                {
                    var oldest = _order.Dequeue();
                    _entries.Remove(oldest);
                }
            }
        }

        return result;
    }
}
