using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Services;

public class TokenService(
    ITwitchAPI api,
    ILogger<TokenService> logger,
    IDbContextFactory<TwitchDbContext> factory
) : BackgroundService
{
    /// <summary>
    /// Аудит №20: семафор был <c>static</c>, поэтому все экземпляры TokenService
    /// (в т.ч. в тест-хосте) делили один лок. Теперь лок принадлежит экземпляру.
    /// </summary>
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private TokenInfo? _tokenInfo;

    /// <summary>
    /// Синхронный доступ оставлен только для совместимости с legacy-консьюмерами.
    /// Он не инициирует сетевой запрос, если кэш ещё актуален, а при необходимости
    /// обновления делегирует <see cref="GetValidTokenAsync"/>. Новый код обязан
    /// использовать асинхронный метод, чтобы не блокировать поток.
    /// </summary>
    public TokenInfo? Token
    {
        get
        {
            var cached = _tokenInfo;

            if (cached != null && IsTokenFresh(cached))
            {
                return cached;
            }

            return GetValidTokenAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        internal set
        {
            if (value != null)
            {
                if (_tokenInfo?.Id != null)
                {
                    value.Id = _tokenInfo.Id;
                }

                _tokenInfo = value;
            }
        }
    }

    public async Task<TokenInfo?> GetTokenAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context.TwitchToken.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RefreshTokenAsync(TokenInfo refreshToken)
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var fakeTwitchApi = new TwitchAPI();
            var result = await fakeTwitchApi.Auth.RefreshAuthTokenAsync(
                refreshToken.RefreshToken,
                api.Settings.Secret,
                api.Settings.ClientId
            );

            var token = await dbContext.TwitchToken.AsNoTracking().SingleAsync();

            token.AccessToken = result.AccessToken;
            token.ExpiresIn = TimeSpan.FromSeconds(result.ExpiresIn);
            token.RefreshToken = result.RefreshToken;
            token.WhenCreated = DateTime.Now.AddSeconds(-30);
            dbContext.TwitchToken.Update(token);

            refreshToken.AccessToken = result.AccessToken;
            refreshToken.ExpiresIn = TimeSpan.FromSeconds(result.ExpiresIn);
            refreshToken.RefreshToken = result.RefreshToken;
            refreshToken.WhenCreated = DateTime.Now.AddSeconds(-30);

            Token = refreshToken;
            api.Settings.AccessToken = result.AccessToken;

            await dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return false;
        }
    }

    public async Task ApplyNewTokenAsync(string accessToken, string refreshToken, int expiresIn)
    {
        await using var dbContext = await factory.CreateDbContextAsync();

        if (await dbContext.TwitchToken.AsNoTracking().AnyAsync())
        {
            var token = await dbContext.TwitchToken.AsNoTracking().SingleAsync();

            token.AccessToken = accessToken;
            token.RefreshToken = refreshToken;
            token.ExpiresIn = TimeSpan.FromSeconds(expiresIn);
            token.WhenCreated = DateTime.Now.AddSeconds(-30);

            Token = token;

            dbContext.Update(Token);
        }
        else
        {
            var tokenInfo = new TokenInfo
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = TimeSpan.FromSeconds(expiresIn),
                WhenCreated = DateTime.Now.AddSeconds(-30),
            };

            await dbContext.AddAsync(tokenInfo);

            Token = tokenInfo;
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Аудит №20: раньше при неудачном refresh возвращался закешированный (протухший)
    /// токен, и вызывающий код уходил в Twitch API с заведомо мёртвым токеном.
    /// Теперь при неудаче возвращается null, чтобы вызывающий увидел явную ошибку.
    /// </summary>
    public async Task<TokenInfo?> GetValidTokenAsync(CancellationToken cancellationToken)
    {
        TokenInfo? result = null;
        var lockTaken = false;

        try
        {
            // Single-flight: параллельные вызовы не должны одновременно слать
            // refresh-запросы к Twitch и получать разные access token.
            if (_tokenInfo == null || !IsTokenFresh(_tokenInfo))
            {
                await _refreshLock.WaitAsync(cancellationToken);
                lockTaken = true;
            }

            var token = _tokenInfo ??= await GetTokenAsync(cancellationToken);

            if (token == null)
            {
                logger.LogWarning("Токен не найден в базе данных");
            }
            else
            {
                var timeUntilExpiry = token.WhenExpires - DateTime.UtcNow;

                if (timeUntilExpiry > TimeSpan.FromMinutes(5))
                {
                    logger.LogInformation(
                        "Токен актуален, истекает через {TimeUntilExpiry}",
                        timeUntilExpiry
                    );
                    result = token;
                }
                else
                {
                    logger.LogInformation(
                        "Токен истекает через {TimeUntilExpiry}, обновляем...",
                        timeUntilExpiry.Negate()
                    );

                    var refreshed = await RefreshTokenAsync(token);

                    if (refreshed)
                    {
                        logger.LogInformation("Токен успешно обновлен");
                        result = token;
                    }
                    else
                    {
                        logger.LogError(
                            "Не удалось обновить токен, возвращается null вместо протухшего токена"
                        );
                        result = null;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            result = null;
        }
        finally
        {
            if (lockTaken)
            {
                _refreshLock.Release();
            }
        }

        return result;
    }

    private static bool IsTokenFresh(TokenInfo token)
    {
        return token.WhenExpires - DateTime.UtcNow > TimeSpan.FromMinutes(5);
    }

    private async Task<TokenInfo> GetFirstTokenAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        TokenInfo? result;

        try
        {
            result = _tokenInfo;

            if (result == null)
            {
                result = await GetTokenAsync(cancellationToken);

                if (result == null)
                {
                    throw new NullReferenceException(nameof(TokenInfo) + " was null");
                }

                if (!IsTokenFresh(result))
                {
                    await RefreshTokenAsync(result);
                }

                _tokenInfo = result;
            }
        }
        finally
        {
            _refreshLock.Release();
        }

        return result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Аудит №20: исключение из старта хоста убивало сервис. Теперь повторяем
        // попытки с задержкой и не блокируем запуск процесса.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _tokenInfo = await GetFirstTokenAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                logger.LogWarning(
                    "Не удалось загрузить токен Twitch, будет повтор через 30 секунд"
                );
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
