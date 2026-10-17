using System;
using System.Threading;
using System.Threading.Tasks;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Services;

public class TokenService(
    ITwitchAPI api,
    ILogger<TokenService> logger,
    IDbContextFactory<TwitchDbContext> factory,
    IConfiguration configuration,
    ITwitchAuthNotifier authNotifier
) : BackgroundService
{
    /// <summary>
    /// Аудит №20: семафор был <c>static</c>, поэтому все экземпляры TokenService
    /// (в т.ч. в тест-хосте) делили один лок. Теперь лок принадлежит экземпляру.
    /// </summary>
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>
    /// Срок токена из конфигурации, когда он не задан: десять лет вместо нуля.
    /// </summary>
    /// <remarks>
    /// Ноль читался бы как «только что истёк» и заставил бы обновлять токен при
    /// каждой проверке, а обновлять нечем — refresh-токена может не быть. Дата
    /// в будущем нужна только чтобы <see cref="IsTokenFresh"/> не отвергла токен.
    /// </remarks>
    private static readonly TimeSpan TokenWithoutExpiry = TimeSpan.FromDays(3650);

    private TokenInfo? _tokenInfo;

    /// <summary>Подсказка о пустом токене печатается один раз, а не каждые 30 секунд.</summary>
    private bool _missingTokenReported;

    private readonly IConfiguration _configuration = configuration;

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

        // Настройки API обновляются и здесь: иначе токен, только что полученный
        // обменом кода, не пошёл бы в вызовы TwitchLib до следующего обновления.
        api.Settings.AccessToken = accessToken;
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

    /// <summary>
    /// Кладёт токен в кэш и в настройки <c>ITwitchAPI</c>.
    /// </summary>
    /// <remarks>
    /// <c>Settings.AccessToken</c> нужен вызовам TwitchLib, которым токен не
    /// передали явно: без него Helix отвечает «Client-ID and OAuth access token
    /// required» даже при живом токене в базе. Заполнялся он только после
    /// обновления, поэтому токен из базы и токен из конфигурации в вызовы не
    /// доходили.
    /// </remarks>
    private void Remember(TokenInfo token)
    {
        _tokenInfo = token;
        api.Settings.AccessToken = token.AccessToken;
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
                    result = await SeedTokenFromConfigurationAsync(cancellationToken);
                }

                if (result == null)
                {
                    throw new NullReferenceException(nameof(TokenInfo) + " was null");
                }

                if (!IsTokenFresh(result))
                {
                    await RefreshTokenAsync(result);
                }

                Remember(result);
            }
        }
        finally
        {
            _refreshLock.Release();
        }

        return result;
    }

    /// <summary>
    /// Первый токен из конфигурации, когда таблица пуста.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Без этого шага первый токен приходилось вносить в базу вручную: других
    /// источников у него нет — из конфигурации в <c>ITwitchAPI</c> приходят только
    /// <c>ClientId</c> и <c>Secret</c>, а <c>Settings.AccessToken</c> выставляется
    /// лишь после удачного обновления. А обновить токен без строки в базе нельзя:
    /// <see cref="RefreshTokenAsync"/> читает именно её.
    /// </para>
    /// <para>
    /// Токен из базы главнее: подставлять конфигурацию поверх действующей
    /// авторизации (и поверх авторизации, полученной через OAuth) нельзя, иначе
    /// правка конфигурации молча отозвала бы её.
    /// </para>
    /// </remarks>
    private async Task<TokenInfo?> SeedTokenFromConfigurationAsync(
        CancellationToken cancellationToken
    )
    {
        TokenInfo? result = null;
        var configured = new TwitchConfiguration();

        _configuration.GetSection(TwitchConfiguration.SectionName).Bind(configured);

        if (!string.IsNullOrWhiteSpace(configured.AccessToken))
        {
            var token = new TokenInfo
            {
                AccessToken = configured.AccessToken,
                RefreshToken = configured.RefreshToken ?? string.Empty,
                ExpiresIn = configured.AccessTokenExpiresIn ?? TokenWithoutExpiry,
                // UtcNow, а не Now: WhenExpires считается в памяти и сравнивается с
                // DateTime.UtcNow, а приведение к UTC делается уже на границе с базой.
                WhenCreated = DateTime.UtcNow,
            };

            await using var context = await factory.CreateDbContextAsync(cancellationToken);

            await context.TwitchToken.AddAsync(token, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            result = token;
            _missingTokenReported = false;

            logger.LogInformation(
                "Таблица TwitchToken пуста — первый токен взят из конфигурации и сохранён в базе (срок {ExpiresIn})",
                token.ExpiresIn
            );
        }
        else if (!_missingTokenReported)
        {
            // Подсказка один раз: цикл старта повторяет попытку каждые 30 секунд,
            // и иначе лог наполнялся бы одним и тем же предупреждением.
            _missingTokenReported = true;

            logger.LogWarning(
                "Токен Twitch не найден ни в базе, ни в конфигурации. Задайте Twitch__AccessToken "
                    + "(и Twitch__RefreshToken, если он есть) в user-secrets или в файле окружения — "
                    + "после этого сервис поднимет токен сам при следующем запуске."
            );

            // Ссылка на переавторизацию уходит один раз вместе с предупреждением:
            // повтор каждые 30 секунд превратил бы уведомление в спам, а по ссылке
            // токен всё равно можно получить один раз.
            await authNotifier.NotifyMissingTokenAsync();
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
