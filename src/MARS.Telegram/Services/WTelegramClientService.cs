using MARS.Telegram.Entities;
using MARS.Telegram.Services.PrivateChannelsResender;

namespace MARS.Telegram.Services;

public class WTelegramClientService(
    ILogger<WTelegramClientService> logger,
    IConfiguration configuration
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

    public Task ReLoginAsync(CancellationToken cancellationToken = default)
    {
        logger.LogWarning("WTelegram ReLogin called in standalone microservice — not supported");
        throw new NotSupportedException(
            "WTelegram авторизация не поддерживается в автономном микросервисе"
        );
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

            var apiId =
                configuration["Telegram:ApiId"]
                ?? configuration["WTelegram:ApiId"]
                ?? Environment.GetEnvironmentVariable("TG_API_ID");
            var apiHash =
                configuration["Telegram:ApiHash"]
                ?? configuration["WTelegram:ApiHash"]
                ?? Environment.GetEnvironmentVariable("TG_API_HASH");
            var phoneNumber =
                configuration["Telegram:PhoneNumber"]
                ?? configuration["WTelegram:PhoneNumber"]
                ?? Environment.GetEnvironmentVariable("TG_PHONE");

            if (string.IsNullOrEmpty(apiId) || string.IsNullOrEmpty(apiHash))
            {
                throw new InvalidOperationException(
                    "WTelegram API credentials not configured (Telegram:ApiId, Telegram:ApiHash)"
                );
            }

            var config = new Dictionary<string, string>
            {
                ["api_id"] = apiId,
                ["api_hash"] = apiHash,
            };

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                config["phone_number"] = phoneNumber;
            }

            _client = new WTelegramClient(key =>
                config.TryGetValue(key, out var value) ? value : null
            );
            await _client.LoginUserIfNeeded();

            logger.LogInformation("WTelegram client initialized successfully");
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

    public Task HandleUpdate(
        global::Telegram.Bot.ITelegramBotClient _,
        global::Telegram.Bot.Types.Update? update
    )
    {
        return Task.CompletedTask;
    }
}
