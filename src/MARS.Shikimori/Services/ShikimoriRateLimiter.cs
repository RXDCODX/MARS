using System.Collections.Concurrent;

namespace MARS.Shikimori.Services;

public class ShikimoriRateLimiter : IShikimoriRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrentRequests, MaxConcurrentRequests);
    private readonly ConcurrentQueue<DateTime> _requestsPerSecond = new();
    private readonly ConcurrentQueue<DateTime> _requestsPerMinute = new();

    private const int MaxRequestsPerSecond = 5;
    private const int MaxRequestsPerMinute = 90;
    private const int MaxConcurrentRequests = 10;

    /// <summary>
    /// Пытается взять слот без ожидания. Слот освобождается сразу после того,
    /// как запрос учтён: освобождать его должен лимитёр, потому что в
    /// <see cref="IShikimoriRateLimiter"/> нет метода Release, а вызывающий код
    /// (<see cref="ShikimoriService"/>) про слоты ничего не знает.
    /// </summary>
    public async Task<bool> TryAcquireAsync()
    {
        var result = false;

        if (await _semaphore.WaitAsync(0))
        {
            try
            {
                if (CanMakeRequest())
                {
                    RecordRequest();
                    result = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return result;
    }

    /// <summary>
    /// Ждёт свободного окна и записывает запрос.
    ///
    /// Слот освобождается в <c>finally</c>, а не только в ветке исключения:
    /// иначе десять запросов подряд исчерпывали бы все слоты параллельности, а
    /// одиннадцатый ждал бы вечно — интерфейс не содержит <c>Release</c>, и
    /// отпускать слот было некому. Такой лимитёр не отвечал бы примерно с
    /// десятого запроса к Shikimori за всё время работы сервиса.
    /// </summary>
    public async Task WaitForSlotAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            while (!CanMakeRequest())
            {
                var delay = CalculateDelay();
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }

            RecordRequest();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public RateLimiterInfo GetInfo()
    {
        var now = DateTime.Now;
        return new RateLimiterInfo
        {
            AvailablePerSecond = CalculateAvailablePerSecond(now),
            AvailablePerMinute = CalculateAvailablePerMinute(now),
            TimeToResetSecond = CalculateTimeToResetSecond(now),
            TimeToResetMinute = CalculateTimeToResetMinute(now),
        };
    }

    private bool CanMakeRequest()
    {
        var now = DateTime.Now;
        CleanupOldRequests(now);
        return _requestsPerSecond.Count < MaxRequestsPerSecond
            && _requestsPerMinute.Count < MaxRequestsPerMinute;
    }

    private void RecordRequest()
    {
        var now = DateTime.Now;
        _requestsPerSecond.Enqueue(now);
        _requestsPerMinute.Enqueue(now);
    }

    private void CleanupOldRequests(DateTime now)
    {
        while (
            _requestsPerSecond.TryPeek(out var requestTime)
            && now - requestTime > TimeSpan.FromSeconds(1)
        )
        {
            _requestsPerSecond.TryDequeue(out _);
        }

        while (
            _requestsPerMinute.TryPeek(out var requestTime)
            && now - requestTime > TimeSpan.FromMinutes(1)
        )
        {
            _requestsPerMinute.TryDequeue(out _);
        }
    }

    private int CalculateAvailablePerSecond(DateTime now)
    {
        CleanupOldRequests(now);
        return Math.Max(0, MaxRequestsPerSecond - _requestsPerSecond.Count);
    }

    private int CalculateAvailablePerMinute(DateTime now)
    {
        CleanupOldRequests(now);
        return Math.Max(0, MaxRequestsPerMinute - _requestsPerMinute.Count);
    }

    private TimeSpan CalculateTimeToResetSecond(DateTime now)
    {
        if (_requestsPerSecond.IsEmpty)
        {
            return TimeSpan.Zero;
        }

        var oldestRequest = _requestsPerSecond.Min();
        var resetTime = oldestRequest.AddSeconds(1);
        return resetTime > now ? resetTime - now : TimeSpan.Zero;
    }

    private TimeSpan CalculateTimeToResetMinute(DateTime now)
    {
        if (_requestsPerMinute.IsEmpty)
        {
            return TimeSpan.Zero;
        }

        var oldestRequest = _requestsPerMinute.Min();
        var resetTime = oldestRequest.AddMinutes(1);
        return resetTime > now ? resetTime - now : TimeSpan.Zero;
    }

    private TimeSpan CalculateDelay()
    {
        var now = DateTime.Now;
        var timeToResetSecond = CalculateTimeToResetSecond(now);
        var timeToResetMinute = CalculateTimeToResetMinute(now);
        return timeToResetSecond < timeToResetMinute ? timeToResetSecond : timeToResetMinute;
    }
}
