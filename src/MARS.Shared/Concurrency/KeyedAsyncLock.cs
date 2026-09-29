namespace MARS.Shared.Concurrency;

/// <summary>
/// Асинхронный мьютекс с ключом: сериализует критические секции по строковому ключу
/// (обычно <c>TwitchId</c>), не блокируя поток и не сериализуя разные ключи между собой.
/// </summary>
/// <remarks>
/// Реализация заменяет прежнюю обёртку <c>SemaphoreSlim</c> со счётчиком использований:
/// там <c>GetOrAdd</c> возвращал семафор, а следующий поток успевал увеличить счётчик
/// уже после того, как другой поток обнулил его и задиспозил семафор. Здесь запись
/// и освобождение записи словаря происходят под одним локом, а удаление возможно только
/// когда на запись не ссылается ни один ожидающий поток, поэтому диспозит безопасен.
/// </remarks>
public sealed class KeyedAsyncLock
{
    private readonly Lock _sync = new();

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// Текущее число ключей, за которыми кто-то заблокирован. Только для диагностики и тестов.
    /// </summary>
    public int TrackedKeyCount
    {
        get
        {
            lock (_sync)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>
    /// Захватывает ключ. Возвращённый <see cref="IDisposable"/> освобождает ключ
    /// при <c>Dispose</c>, в том числе если тело критической секции бросило исключение.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Entry entry;

        lock (_sync)
        {
            if (!_entries.TryGetValue(key, out var existing))
            {
                existing = new Entry();
                _entries[key] = existing;
            }

            entry = existing;
            entry.RefCount++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Захват не состоялся, семафор по-прежнему свободен: отпускать его нельзя,
            // только снять ссылку на запись.
            RemoveReference(key, entry);
            throw;
        }

        return new Releaser(this, key, entry);
    }

    /// <summary>
    /// Освобождает захваченный ключ: сначала отпускает семафор (чтобы следующий
    /// ожидающий поток проснулся), затем снимает ссылку на запись. Порядок важен:
    /// при обратном порядке удалённая и задиспозитленная запись использовалась бы
    /// уже ожидающим потоком.
    /// </summary>
    private void Release(string key, Entry entry)
    {
        entry.Semaphore.Release();
        RemoveReference(key, entry);
    }

    private void RemoveReference(string key, Entry entry)
    {
        lock (_sync)
        {
            entry.RefCount--;

            if (entry.RefCount == 0 && _entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
            {
                _entries.Remove(key);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        /// <summary>
        /// Число потоков, которые уже получили ссылку на эту запись (ждут семафор или держат его).
        /// Управляется только под локом <see cref="KeyedAsyncLock._sync"/>.
        /// </summary>
        public int RefCount { get; set; }
    }

    private sealed class Releaser(KeyedAsyncLock owner, string key, Entry entry) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                owner.Release(key, entry);
            }
        }
    }
}
