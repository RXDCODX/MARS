using MARS.Telegram.Entities;
using MARS.Telegram.Services.PrivateChannelsResender;
using MARS.Telegram.Services.WTelegram;

namespace MARS.Telegram.Services;

public class WTelegramClientService(
    IWTelegramClientFactory factory,
    ILogger<WTelegramClientService> logger
) : IWTelegramClientService
{
    private WTelegramClient? _client;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public Task<WTelegramClientStatus> GetClientStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        var isAuthenticated = _client?.User != null;
        return Task.FromResult(
            new WTelegramClientStatus { IsAuthenticated = isAuthenticated, IsAwaitingCode = false }
        );
    }

    /// <summary>
    /// Переподключение после потери сессии: клиент закрывается и входит заново.
    /// </summary>
    /// <remarks>
    /// Раньше вход был невозможен в принципе — метод бросал
    /// <c>NotSupportedException</c>, и канал работал только пока жила сессия в
    /// памяти. Теперь сессия лежит в БД, а вход идёт через тот же прокси, что и у
    /// бота, поэтому переподключиться можно без участия человека.
    /// </remarks>
    public async Task ReLoginAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);

        try
        {
            DisposeClient();
            _client = null;
        }
        finally
        {
            _initLock.Release();
        }

        await GetClientAsync(cancellationToken);
    }

    public bool SubmitVerificationCode(string code)
    {
        logger.LogWarning(
            "WTelegram SubmitVerificationCode called in standalone microservice — not supported"
        );
        return false;
    }

    public async Task<IWTelegramChannelClient> GetClientAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_client is { User: not null })
        {
            return new WTelegramChannelClient(_client);
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_client is { User: not null })
            {
                return new WTelegramChannelClient(_client);
            }

            _client = await factory.LoginAsUserAsync(cancellationToken);

            return new WTelegramChannelClient(_client);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize WTelegram client");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void DisposeClient()
    {
        if (_client is { } client)
        {
            client.Dispose();
        }
    }
}
