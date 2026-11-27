using System.Reflection;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Core;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Tests.Services;

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
            new ConfigurationBuilder().Build(),
            new SilentNotifier(),
            TimeProvider.System
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
    /// Пустая таблица не создаёт токен из конфигурации: владельцем авторизации
    /// является OAuth-флоу. Вместо этого сервис запрашивает переавторизацию.
    /// </summary>
    [Fact]
    public async Task Пустая_таблица_запрашивает_авторизацию_и_не_берёт_токен_из_конфигурации()
    {
        using var stopping = new CancellationTokenSource();
        var notifier = new CountingAuthNotifier();
        var service = Create(
            Configuration(
                ("Twitch:AccessToken", "access-из-конфигурации"),
                ("Twitch:RefreshToken", "refresh-из-конфигурации")
            ),
            new CancellingLogger(stopping),
            notifier: notifier
        );

        await ExecuteAsync(service, stopping.Token);

        Assert.Equal(1, notifier.Calls);
        Assert.Null(service.Token);
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Empty(db.TwitchToken);
    }

    /// <summary>
    /// Срок токена из базы сохраняется: конфигурация больше не является
    /// источником токенов и не должна перезаписывать OAuth-данные.
    /// </summary>
    [Fact]
    public async Task Срок_токена_из_базы_сохраняется()
    {
        await SeedAsync("access-из-базы", TimeSpan.FromHours(2));
        var service = Create(Configuration(("Twitch:AccessTokenExpiresIn", "05:00:00")));

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromHours(2), db.TwitchToken.AsNoTracking().Single().ExpiresIn);
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
    /// Без токена в базе сервис должен указать на OAuth-авторизацию, а не на
    /// удалённую настройку токена в конфигурации.
    /// </summary>
    [Fact]
    public async Task Без_токена_в_базе_даёт_подсказку()
    {
        using var stopping = new CancellationTokenSource();
        var logger = new CancellingLogger(stopping);
        var service = Create(Configuration(), logger);

        await ExecuteAsync(service, stopping.Token);

        Assert.Contains(
            logger.Messages,
            message => message.Contains("/twitch-auth", StringComparison.Ordinal)
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

    /// <summary>
    /// Без токена нигде уходит ссылка на переавторизацию: ждать, пока владелец сам
    /// посмотрит лог, — значит оставить стенд без событий Twitch на неизвестное время.
    /// </summary>
    [Fact]
    public async Task Без_токена_уходит_уведомление_о_переавторизации()
    {
        using var stopping = new CancellationTokenSource();
        var notifier = new CountingAuthNotifier();
        var service = Create(Configuration(), new CancellingLogger(stopping), notifier: notifier);

        await ExecuteAsync(service, stopping.Token);

        Assert.Equal(1, notifier.Calls);
    }

    /// <summary>
    /// Просьба не повторяется, пока не прошёл интервал: цикл старта повторяет
    /// попытку каждые 30 секунд, и без интервала уведомления шли бы непрерывной
    /// очередью.
    /// </summary>
    [Fact]
    public async Task Уведомление_не_повторяется_раньше_интервала()
    {
        using var stopping = new CancellationTokenSource();
        var notifier = new CountingAuthNotifier();
        var clock = new FakeTimeProvider();
        var service = Create(
            Configuration(),
            new CancellingLogger(stopping),
            notifier: notifier,
            timeProvider: clock
        );

        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        await ExecuteAsync(service, stopping.Token);
        clock.Advance(TimeSpan.FromHours(12));
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal(1, notifier.Calls);
    }

    /// <summary>
    /// При недоставке сообщение повторяется через минуту: например, MARS.TeleGRAM
    /// может ещё запускаться одновременно с TwitchCore, и потерянную первую попытку
    /// нельзя превращать в суточное ожидание.
    /// </summary>
    [Fact]
    public async Task Недоставленное_уведомление_повторяется_через_минуту()
    {
        var notifier = new RetryingAuthNotifier();
        var clock = new FakeTimeProvider();
        var service = Create(Configuration(), notifier: notifier, timeProvider: clock);

        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        Assert.Equal(1, notifier.Calls);

        clock.Advance(TimeSpan.FromSeconds(30));
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        Assert.Equal(1, notifier.Calls);

        clock.Advance(TimeSpan.FromSeconds(31));
        notifier.Deliver = true;
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        Assert.Equal(2, notifier.Calls);

        clock.Advance(TimeSpan.FromHours(12));
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        Assert.Equal(2, notifier.Calls);
    }

    /// <summary>
    /// Через сутки после успешной доставки просьба повторяется.
    /// </summary>
    [Fact]
    public async Task Уведомление_повторяется_через_сутки()
    {
        var notifier = new CountingAuthNotifier();
        var clock = new FakeTimeProvider();
        var service = Create(Configuration(), notifier: notifier, timeProvider: clock);

        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromHours(25));
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal(2, notifier.Calls);
    }

    /// <summary>
    /// Интервал задаётся конфигурацией: сутки — для боевого стенда, а на
    /// отладке повтор хочется видеть чаще.
    /// </summary>
    [Fact]
    public async Task Интервал_повтора_берётся_из_конфигурации()
    {
        var notifier = new CountingAuthNotifier();
        var clock = new FakeTimeProvider();
        var service = Create(
            Configuration(("Twitch:AuthRequestInterval", "00:05:00")),
            notifier: notifier,
            timeProvider: clock
        );

        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(6));
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal(2, notifier.Calls);
    }

    /// <summary>
    /// Мусорное значение не должно превращать интервал в ноль: при нуле просьба
    /// уходила бы на каждой попытке цикла старта, то есть каждые 30 секунд.
    /// </summary>
    [Fact]
    public async Task Пустой_интервал_берётся_из_умолчания()
    {
        var notifier = new CountingAuthNotifier();
        var clock = new FakeTimeProvider();
        var service = Create(
            Configuration(("Twitch:AuthRequestInterval", "00:00:00")),
            notifier: notifier,
            timeProvider: clock
        );

        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(10));
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal(1, notifier.Calls);
    }

    /// <summary>
    /// Токен появился — отметка о просьбе сбрасывается, и при следующем протухании
    /// спросить надо сразу, а не ждать сутки после прошлой просьбы.
    /// </summary>
    [Fact]
    public async Task Появление_токена_сбрасывает_отметку()
    {
        var notifier = new CountingAuthNotifier();
        var service = Create(notifier: notifier);

        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);
        Assert.Equal(1, notifier.Calls);

        await service.ApplyNewTokenAsync("токен-из-oauth", "refresh-из-oauth", 3600);
        await RequestAuthorizationAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal(2, notifier.Calls);
    }

    /// <summary>
    /// Токен из базы поднимается сам — переавторизация не нужна, и лишнее
    /// сообщение только сбивало бы с толку.
    /// </summary>
    [Fact]
    public async Task С_токеном_из_базы_уведомление_не_уходит()
    {
        await SeedAsync("access-из-базы");
        var notifier = new CountingAuthNotifier();
        var service = Create(notifier: notifier);

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal(0, notifier.Calls);
    }

    private static async Task RequestAuthorizationAsync(
        TokenService service,
        CancellationToken cancellationToken
    )
    {
        var method = typeof(TokenService).GetMethod(
            "RequestAuthorizationAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        await (Task)method.Invoke(service, [cancellationToken])!;
    }

    /// <summary>
    /// Часы, которые двигает тест: ждать сутки ради проверки интервала нельзя.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }

    /// <summary>Нотификатор, считающий успешные доставки.</summary>
    private sealed class CountingAuthNotifier : ITwitchAuthNotifier
    {
        public int Calls { get; private set; }

        public Task<bool> NotifyMissingTokenAsync()
        {
            Calls++;

            return Task.FromResult(true);
        }
    }

    /// <summary>Сначала имитирует недоступность Telegram, затем успешную доставку.</summary>
    private sealed class RetryingAuthNotifier : ITwitchAuthNotifier
    {
        public int Calls { get; private set; }

        public bool Deliver { get; set; }

        public Task<bool> NotifyMissingTokenAsync()
        {
            Calls++;

            return Task.FromResult(Deliver);
        }
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
        ITwitchAPI? api = null,
        ITwitchAuthNotifier? notifier = null,
        TimeProvider? timeProvider = null
    ) =>
        new(
            api ?? CreateApi(new ApiSettings()),
            logger ?? NullLogger<TokenService>.Instance,
            _factory,
            configuration ?? new ConfigurationBuilder().Build(),
            notifier ?? new SilentNotifier(),
            timeProvider ?? TimeProvider.System
        );

    /// <summary>Нотификатор-заглушка для тестов, которым всё равно.</summary>
    private sealed class SilentNotifier : ITwitchAuthNotifier
    {
        public Task<bool> NotifyMissingTokenAsync() => Task.FromResult(true);
    }

    private async Task SeedAsync(string accessToken, TimeSpan? expiresIn = null)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.TwitchToken.Add(
            new TokenInfo
            {
                AccessToken = accessToken,
                RefreshToken = "refresh",
                ExpiresIn = expiresIn ?? TimeSpan.FromHours(1),
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
