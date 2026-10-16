using System.Reflection;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Core;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Хранение токена Twitch.
///
/// Сервис сам поднимает токен при старте и повторяет попытку, если Twitch временно
/// недоступен: без этого весь сервис остался бы без событий до перезапуска. Токен
/// обновляется только по требованию — самопроизвольная смена токена отозвала бы
/// авторизацию.
/// </summary>
public class TokenServiceStartTests
{
    private readonly TwitchTestDbContextFactory _factory = new();

    /// <summary>
    /// Токен из базы поднимается при старте: без него сервис не обращается к Twitch
    /// вообще.
    /// </summary>
    [Fact]
    public async Task StartupLoadsTokenFromDatabase()
    {
        var factory = new TwitchTestDbContextFactory();
        await using (
            var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            db.TwitchToken.Add(
                new TokenInfo
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    ExpiresIn = TimeSpan.FromHours(1),
                    WhenCreated = DateTime.Now,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var service = new TokenService(
            Mock.Of<ITwitchAPI>(),
            NullLogger<TokenService>.Instance,
            factory,
            new ConfigurationBuilder().Build()
        );

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal("access-token", service.Token!.AccessToken);
    }

    /// <summary>
    /// Новый токен заменяет старый, а не добавляется вторым: иначе сервис держал бы
    /// в базе отозванные токены и выбрал бы не тот.
    /// </summary>
    [Fact]
    public async Task NewTokenReplacesStoredOne()
    {
        await SeedAsync("старый токен");
        var service = Create();

        await service.ApplyNewTokenAsync("новый токен", "новый refresh", 3600);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var stored = db.TwitchToken.AsNoTracking().Single();

        Assert.Equal("новый токен", stored.AccessToken);
        Assert.Equal(TimeSpan.FromSeconds(3600), stored.ExpiresIn);
    }

    /// <summary>
    /// Токен без прежнего кладётся как есть: обновлять нечего.
    /// </summary>
    [Fact]
    public async Task FirstTokenIsStoredAsIs()
    {
        var service = Create();

        await service.ApplyNewTokenAsync("первый токен", "первый refresh", 3600);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal("первый токен", db.TwitchToken.AsNoTracking().Single().AccessToken);
    }

    /// <summary>
    /// Пустая таблица не оставляет сервис без токена: он берёт первый токен из
    /// конфигурации и кладёт его в базу. Без этого владельцу приходилось вносить
    /// токен в базу вручную, а без строки обновлять его было нечем — refresh
    /// требует существующей строки.
    /// </summary>
    [Fact]
    public async Task Пустая_таблица_берёт_первый_токен_из_конфигурации()
    {
        var service = Create(
            Configuration(
                ("Twitch:AccessToken", "access-из-конфигурации"),
                ("Twitch:RefreshToken", "refresh-из-конфигурации")
            )
        );

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal("access-из-конфигурации", service.Token!.AccessToken);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var stored = db.TwitchToken.AsNoTracking().Single();

        Assert.Equal("access-из-конфигурации", stored.AccessToken);
        Assert.Equal("refresh-из-конфигурации", stored.RefreshToken);
    }

    /// <summary>
    /// Срок из конфигурации уважается: без него токен считался бы протухшим при
    /// первой же проверке и сервис сразу пошёл бы в обновление.
    /// </summary>
    [Fact]
    public async Task Срок_из_конфигурации_переносится_в_базу()
    {
        var service = Create(
            Configuration(
                ("Twitch:AccessToken", "access-из-конфигурации"),
                ("Twitch:AccessTokenExpiresIn", "02:00:00")
            )
        );

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromHours(2), db.TwitchToken.AsNoTracking().Single().ExpiresIn);
    }

    /// <summary>
    /// Без срока токен считается бессрочным, а не протухшим: неизвестный срок
    /// не должен превращаться в нулевой и вызывать обновление каждый раз.
    /// </summary>
    [Fact]
    public async Task Без_срока_токен_считается_бессрочным()
    {
        var service = Create(Configuration(("Twitch:AccessToken", "access-из-конфигурации")));

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var stored = db.TwitchToken.AsNoTracking().Single();

        Assert.True(stored.WhenExpires > DateTime.UtcNow.AddYears(1));
    }

    /// <summary>
    /// Токен из базы главнее конфигурации: иначе правка конфигурации молча
    /// подменяла бы уже действующую авторизацию (и авторизацию, полученную через
    /// OAuth, — тоже).
    /// </summary>
    [Fact]
    public async Task Токен_из_базы_не_затирается_конфигурацией()
    {
        await SeedAsync("токен-из-базы");
        var service = Create(Configuration(("Twitch:AccessToken", "access-из-конфигурации")));

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal("токен-из-базы", service.Token!.AccessToken);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal("токен-из-базы", db.TwitchToken.AsNoTracking().Single().AccessToken);
    }

    /// <summary>
    /// Ни токена в базе, ни в конфигурации — сервис должен сказать, что именно
    /// задать. Молчаливый отпуск и выглядел как «токен есть, но не работает».
    /// </summary>
    [Fact]
    public async Task Без_токена_в_базе_и_конфигурации_даёт_подсказку()
    {
        using var stopping = new CancellationTokenSource();
        var logger = new CancellingLogger(stopping);
        var service = Create(Configuration(), logger);

        await ExecuteAsync(service, stopping.Token);

        Assert.Contains(
            logger.Messages,
            message => message.Contains("Twitch__AccessToken", StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Токен обязан попасть в настройки API: TwitchLib берёт access token оттуда,
    /// когда вызов не передал его явно, и без него Helix отвечает «Client-ID and
    /// OAuth access token required» — даже при живом токене в базе.
    /// </summary>
    [Fact]
    public async Task Токен_из_базы_попадает_в_настройки_API()
    {
        await SeedAsync("access-из-базы");
        var settings = new ApiSettings { ClientId = "client-id" };
        var service = Create(api: CreateApi(settings));

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal("access-из-базы", settings.AccessToken);
    }

    /// <summary>
    /// То же после авторизации через OAuth: иначе токен, только что полученный
    /// обменом кода, не пошёл бы в вызовы TwitchLib до следующего обновления.
    /// </summary>
    [Fact]
    public async Task Новый_токен_попадает_в_настройки_API()
    {
        var settings = new ApiSettings();
        var service = Create(api: CreateApi(settings));

        await service.ApplyNewTokenAsync("access-из-oauth", "refresh-из-oauth", 3600);

        Assert.Equal("access-из-oauth", settings.AccessToken);
    }

    [Fact]
    public async Task StartupWithoutTokenDoesNotThrow()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await ExecuteAsync(Create(), stopping.Token);
    }

    private static ITwitchAPI CreateApi(ApiSettings settings)
    {
        var api = new Mock<ITwitchAPI>();

        api.SetupGet(instance => instance.Settings).Returns(settings);

        return api.Object;
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                settings.Select(setting => new KeyValuePair<string, string?>(
                    setting.Key,
                    setting.Value
                ))
            )
            .Build();

    private TokenService Create(
        IConfiguration? configuration = null,
        ILogger<TokenService>? logger = null,
        ITwitchAPI? api = null
    ) =>
        new(
            api ?? CreateApi(new ApiSettings()),
            logger ?? NullLogger<TokenService>.Instance,
            _factory,
            configuration ?? new ConfigurationBuilder().Build()
        );

    private async Task SeedAsync(string accessToken)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.TwitchToken.Add(
            new TokenInfo
            {
                AccessToken = accessToken,
                RefreshToken = "refresh",
                ExpiresIn = TimeSpan.FromHours(1),
                WhenCreated = DateTime.Now,
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Task ExecuteAsync(TokenService service, CancellationToken stoppingToken)
    {
        var method = typeof(TokenService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(service, [stoppingToken])!;
    }

    /// <summary>
    /// Логгер, который гасит токен на первом предупреждении: цикл старта повторяет
    /// попытку через 30 секунд, а тесту ждать нечего — он проверяет сообщение.
    /// </summary>
    private sealed class CancellingLogger(CancellationTokenSource stopping) : ILogger<TokenService>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Messages.Add(formatter(state, exception));

            if (logLevel == LogLevel.Warning)
            {
                stopping.Cancel();
            }
        }
    }
}
