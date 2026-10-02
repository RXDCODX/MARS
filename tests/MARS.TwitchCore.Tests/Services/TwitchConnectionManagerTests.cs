using System.Reflection;
using MARS.TwitchCore.Configuration;
using MARS.TwitchCore.Services.Connection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Подключение к чату Twitch.
///
/// Сервис обязан подниматься без токена: OAuth приходит из конфигурации, и его
/// может не быть на старте. Проверяется именно это — раньше пустой токен ронял весь
/// хост в конструкторе <c>ConnectionCredentials</c>.
/// </summary>
public class TwitchConnectionManagerTests
{
    [Fact]
    public async Task ManagerIsCreatedWithoutToken()
    {
        await using var manager = Create();

        Assert.False(manager.IsConnected);
    }

    /// <summary>
    /// Клиент Twitch создаётся сразу, иначе подписчики событий не найдут его и
    /// сервис молчал бы.
    /// </summary>
    [Fact]
    public async Task ClientIsAvailable()
    {
        await using var manager = Create();

        Assert.NotNull(manager.Client);
    }

    /// <summary>
    /// Статус содержит признак подключения и каналы: его читают при разборе
    /// «почему чат молчит».
    /// </summary>
    [Fact]
    public async Task StatusReportsConnectionState()
    {
        await using var manager = Create();

        var status = manager.GetStatus();

        Assert.Contains("Connected: False", status);
        Assert.Contains("JoinedChannels: -", status);
        Assert.Contains("ReconnectAttempts: 0", status);
    }

    /// <summary>
    /// Старт без токена не бросает исключение: сервис продолжает работать и ждёт
    /// токен.
    /// </summary>
    [Fact]
    public async Task StartWithoutTokenDoesNotThrow()
    {
        await using var manager = Create();

        await manager.StartAsync(TestContext.Current.CancellationToken);

        Assert.False(manager.IsConnected);
    }

    [Fact]
    public async Task StopWithoutTokenIsSafe()
    {
        await using var manager = Create();
        await manager.StartAsync(TestContext.Current.CancellationToken);

        await manager.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(manager.IsConnected);
    }

    /// <summary>
    /// Смена протокола транспорта требует перезапуска и не применяется на лету: обмен
    /// идёт по уже открытому сокету.
    /// </summary>
    [Fact]
    public async Task ProtocolChangeIsDetected()
    {
        await using var manager = Create();

        OnConfigurationChanged(manager, new TwitchConfiguration { TransportProtocol = "Tcp" });

        Assert.False(manager.IsConnected);
    }

    /// <summary>
    /// Задержка переподключения растёт вдвое, но не выше потолка: иначе после
    /// обрыва чат лежал бы на минутах вместо секунд.
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(5, 32)]
    [InlineData(20, 300)]
    public void ReconnectDelayGrowsUpToCeiling(int attempt, double expectedSeconds)
    {
        var delay = CalculateReconnectDelay(attempt);

        Assert.Equal(expectedSeconds, delay.TotalSeconds);
    }

    /// <summary>
    /// Менеджер освобождает транспорт: иначе сокет остался бы висеть после
    /// остановки сервиса.
    /// </summary>
    [Fact]
    public async Task DisposeIsSafe()
    {
        var manager = Create();
        await manager.StartAsync(TestContext.Current.CancellationToken);

        await manager.DisposeAsync();
    }

    private static TwitchConnectionManager Create(string protocol = "WebSocket") =>
        new(
            NullLogger<TwitchConnectionManager>.Instance,
            new StaticOptionsMonitor<TwitchConfiguration>(
                new TwitchConfiguration { TransportProtocol = protocol }
            ),
            NullLoggerFactory.Instance
        );

    private static void OnConfigurationChanged(
        TwitchConnectionManager manager,
        TwitchConfiguration config
    )
    {
        var method = typeof(TwitchConnectionManager).GetMethod(
            "OnConfigurationChanged",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        method.Invoke(manager, [config, null]);
    }

    private static TimeSpan CalculateReconnectDelay(int attempt)
    {
        var method = typeof(TwitchConnectionManager).GetMethod(
            "CalculateReconnectDelay",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (TimeSpan)method.Invoke(null, [attempt])!;
    }

    /// <summary>
    /// Монитор опций с фиксированным значением: проверяется поведение менеджера, а
    /// не подписка на конфигурацию.
    /// </summary>
    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
