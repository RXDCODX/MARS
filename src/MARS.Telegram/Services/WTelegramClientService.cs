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
        var result = new WTelegramClientStatus
        {
            IsAwaitingCode = factory.AwaitingCode,
            ErrorMessage = factory.LastLoginError,
        };
        var user = _client?.User;

        if (user is not null)
        {
            result.IsAuthenticated = true;
            result.UserId = user.ID;
            result.Username = Username(user);
            result.Phone = string.IsNullOrWhiteSpace(user.phone) ? null : user.phone;
        }

        return Task.FromResult(result);
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

    /// <summary>
    /// Принимает код подтверждения, присланный с веб-страницы.
    /// </summary>
    /// <remarks>
    /// Раньше метод отказывал всегда, а страница «WTelegram Клиент» при этом
    /// показывала форму ввода кода: код было некуда вводить, и первый вход
    /// пользовательского аккаунта был невозможен в принципе. Теперь код уходит в
    /// фабрику и уходит в клиент при следующей попытке входа, которую инициирует
    /// контроллер.
    /// </remarks>
    public bool SubmitVerificationCode(string code)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(code))
        {
            factory.SubmitLoginCode(code.Trim());
            logger.LogInformation("Код подтверждения входа принят, ждёт новой попытки входа");
            result = true;
        }
        else
        {
            logger.LogWarning("Код подтверждения входа пуст, принят не будет");
        }

        return result;
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

    /// <summary>Имя аккаунта для журнала и страницы статуса.</summary>
    private static string? Username(TL.User? user)
    {
        var names = user?.ActiveUsernames;

        return names is null ? null : string.Join(", ", names);
    }

    private void DisposeClient()
    {
        if (_client is { } client)
        {
            client.Dispose();
        }
    }
}
