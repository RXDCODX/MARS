using System.Threading;
using MARS.Twitch.Application.Configuration;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;

namespace MARS.Twitch.Infrastructure.Services.Connection;

public class TwitchConnectionManager : IHostedService, IAsyncDisposable, ITwitchConnectionState
{
    private readonly ILogger<TwitchConnectionManager> _logger;
    private readonly IOptionsMonitor<TwitchConfiguration> _twitchOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TokenService? _tokenService;

    private readonly TwitchClient _client;
    private readonly IClient _transport;
    private ConnectionCredentials? _credentials;
    private string _currentOAuth;
    private string _botName;
    private readonly IDisposable? _changeToken;
    private bool _initialized;

    public bool IsConnected => _client.IsConnected;

    public string JoinedChannels =>
        string.Join(", ", _client.JoinedChannels.Select(c => c.Channel));

    public DateTime? LastConnectedAt => _lastConnectedAt;

    public DateTime? LastDisconnectedAt => _lastDisconnectedAt;

    public string? LastDisconnectReason => _lastDisconnectReason;

    public int ReconnectAttempts => _reconnectAttempts;

    public bool IsReconnecting => _isReconnecting;

    private DateTime? _lastConnectedAt;
    private DateTime? _lastDisconnectedAt;
    private string? _lastDisconnectReason;

    private CancellationTokenSource? _reconnectCts;
    private Task? _reconnectTask;
    private int _reconnectAttempts;
    private bool _isReconnecting;
    private bool _manualDisconnect;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private const int MaxReconnectAttempts = 10;
    private const int BaseDelaySeconds = 2;
    private const int MaxDelaySeconds = 300;

    public ITwitchClient Client => _client;

    public TwitchConnectionManager(
        ILogger<TwitchConnectionManager> logger,
        IOptionsMonitor<TwitchConfiguration> twitchOptions,
        ILoggerFactory loggerFactory,
        TokenService? tokenService = null
    )
    {
        _logger = logger;
        _twitchOptions = twitchOptions;
        _loggerFactory = loggerFactory;
        _tokenService = tokenService;

        var config = twitchOptions.CurrentValue;

        _transport = CreateTransportClient(config.TransportProtocol);
        _client = CreateTwitchClient(config.TransportProtocol);

        // IRC OAuth берётся из БД (TokenService), если там есть валидный токен
        // с chat:read. Иначе — из конфигурации (Twitch__OAuth). TokenService уже
        // обновляет токен через refresh token и хранит его в БД.
        _currentOAuth = config.OAuth ?? string.Empty;
        _botName = TwitchConstants.BotName;

        _changeToken = twitchOptions.OnChange(OnConfigurationChanged);

        WireEvents();
    }

    private ConnectionCredentials? TryCreateCredentials(string? oauth)
    {
        if (string.IsNullOrWhiteSpace(oauth))
        {
            _logger.LogWarning(
                "Twitch OAuth не задан (Twitch__OAuth). Подключение к чату будет отложено "
                    + "до появления корректного токена; остальная часть сервиса работает."
            );
            return null;
        }

        return new ConnectionCredentials(_botName, oauth);
    }

    private IClient CreateTransportClient(string protocol)
    {
        var options = new ClientOptions(
            reconnectionPolicy: new NoReconnectionPolicy(),
            useSsl: true,
            disconnectWait: 1500,
            clientType: ClientType.Chat
        );

        if (string.Equals(protocol, "Tcp", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using TCP transport for Twitch connection");
            return new TcpClient(options, _loggerFactory.CreateLogger<TcpClient>());
        }

        _logger.LogInformation("Using WebSocket transport for Twitch connection");
        return new WebSocketClient(options, _loggerFactory.CreateLogger<WebSocketClient>());
    }

    private TwitchClient CreateTwitchClient(string protocol)
    {
        var isTcp = string.Equals(protocol, "Tcp", StringComparison.OrdinalIgnoreCase);

        return new TwitchClient(
            client: _transport,
            protocol: isTcp ? ClientProtocol.TCP : ClientProtocol.WebSocket,
            sendOptions: new SendOptions(),
            loggerFactory: _loggerFactory
        );
    }

    private void WireEvents()
    {
        _client.OnConnected += (_, _) =>
        {
            _lastConnectedAt = DateTime.Now;
            _reconnectAttempts = 0;
            _isReconnecting = false;
            _logger.LogInformation("Twitch chat connected as {Bot}", _botName);
            return Task.CompletedTask;
        };

        _client.OnDisconnected += (_, _) =>
        {
            _lastDisconnectedAt = DateTime.Now;
            _logger.LogWarning("Twitch chat disconnected");

            if (!_manualDisconnect && !_isReconnecting)
            {
                _ = TryReconnectAsync();
            }

            return Task.CompletedTask;
        };

        _client.OnConnectionError += (_, args) =>
        {
            _lastDisconnectedAt = DateTime.Now;
            _lastDisconnectReason = args.Error?.Message ?? "Unknown";
            _logger.LogError("Twitch connection error: {Message}", args.Error?.Message);

            if (!_manualDisconnect && !_isReconnecting)
            {
                _ = TryReconnectAsync();
            }

            return Task.CompletedTask;
        };
    }

    private void OnConfigurationChanged(TwitchConfiguration config, string? name)
    {
        var currentProtocol = _transport is TcpClient ? "Tcp" : "WebSocket";

        if (
            !string.Equals(
                currentProtocol,
                config.TransportProtocol,
                StringComparison.OrdinalIgnoreCase
            ) && !_manualDisconnect
        )
        {
            _logger.LogWarning(
                "Twitch transport protocol change from {Old} to {New} requires service restart.",
                currentProtocol,
                config.TransportProtocol
            );
        }

        if (
            !string.Equals(_currentOAuth, config.OAuth, StringComparison.Ordinal)
            && !_manualDisconnect
        )
        {
            _currentOAuth = config.OAuth ?? string.Empty;
            _credentials = TryCreateCredentials(_currentOAuth);

            if (_client.IsConnected)
            {
                _logger.LogInformation("Twitch OAuth token changed, reconnecting...");
                _ = ReconnectAsync();
            }
            else if (_credentials != null)
            {
                // Раньше клиент ещё не подключался — просто пробуем подключиться
                // с новым токеном, вместо того чтобы ждать ручного рестарта.
                _ = EnsureInitializedAndConnected();
            }
        }
    }

    private Timer? _healthCheckTimer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _reconnectCts = new CancellationTokenSource();
            _manualDisconnect = false;
            await EnsureInitializedAndConnected();

            // TwitchLib не генерирует OnDisconnected при NormalClosure (чистое
            // закрытие WebSocket), поэтому наша логика переподключения не
            // срабатывает. Периодическая проверка обнаруживает разрыв и
            // запускает переподключение.
            _healthCheckTimer = new Timer(
                _ => _ = HealthCheckAsync(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect Twitch client on startup");
        }
    }

    private async Task HealthCheckAsync()
    {
        if (_manualDisconnect)
        {
            return;
        }

        if (!_client.IsConnected)
        {
            _logger.LogWarning("Twitch chat disconnected (health check), attempting reconnect...");
            try
            {
                await EnsureInitializedAndConnected();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check reconnect failed");
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
            _manualDisconnect = true;
            await _reconnectCts?.CancelAsync()!;

            if (_reconnectTask != null)
            {
                await _reconnectTask;
            }

            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }

            _reconnectCts?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping Twitch client");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _changeToken?.Dispose();

        await StopAsync(CancellationToken.None);

        (_transport as IDisposable)?.Dispose();
        _reconnectLock.Dispose();
        _connectLock.Dispose();
    }

    public string GetStatus()
    {
        var joined = string.Join(", ", _client.JoinedChannels.Select(c => c.Channel));
        return $"Connected: {IsConnected}\n"
            + $"JoinedChannels: {(string.IsNullOrWhiteSpace(joined) ? "-" : joined)}\n"
            + $"LastConnectedAt: {_lastConnectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}\n"
            + $"LastDisconnectedAt: {_lastDisconnectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}\n"
            + $"LastDisconnectReason: {_lastDisconnectReason ?? "-"}\n"
            + $"ReconnectAttempts: {_reconnectAttempts}\n"
            + $"IsReconnecting: {_isReconnecting}";
    }

    public async Task<bool> ReconnectAsync()
    {
        try
        {
            _logger.LogInformation("Manual Twitch reconnect requested");

            _manualDisconnect = true;
            await _reconnectCts?.CancelAsync()!;

            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));

            _reconnectAttempts = 0;
            _manualDisconnect = false;
            _reconnectCts = new CancellationTokenSource();

            await EnsureInitializedAndConnected();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect Twitch client");
            return false;
        }
    }

    private async Task TryReconnectAsync()
    {
        await _reconnectLock.WaitAsync();
        try
        {
            if (
                _isReconnecting
                || _manualDisconnect
                || _reconnectCts?.Token.IsCancellationRequested == true
            )
            {
                return;
            }

            _isReconnecting = true;
            _reconnectTask = ReconnectLoopAsync(_reconnectCts!.Token);
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (
            !cancellationToken.IsCancellationRequested && _reconnectAttempts < MaxReconnectAttempts
        )
        {
            _reconnectAttempts++;

            var delay = CalculateReconnectDelay(_reconnectAttempts);
            _logger.LogInformation(
                "Attempting to reconnect to Twitch (attempt {Attempt}/{Max}) in {Delay} seconds...",
                _reconnectAttempts,
                MaxReconnectAttempts,
                delay.TotalSeconds
            );

            try
            {
                await Task.Delay(delay, cancellationToken);

                if (_client.IsConnected)
                {
                    _logger.LogInformation("Already connected, stopping reconnect attempts");
                    _isReconnecting = false;
                    return;
                }

                await EnsureInitializedAndConnected();

                if (_client.IsConnected)
                {
                    _logger.LogInformation("Successfully reconnected to Twitch");
                    _isReconnecting = false;
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Reconnect cancelled");
                _isReconnecting = false;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnect attempt {Attempt} failed", _reconnectAttempts);
            }
        }

        if (_reconnectAttempts >= MaxReconnectAttempts)
        {
            _logger.LogError(
                "Failed to reconnect to Twitch after {MaxAttempts} attempts.",
                MaxReconnectAttempts
            );
        }

        _isReconnecting = false;
    }

    private static TimeSpan CalculateReconnectDelay(int attempt)
    {
        var delaySeconds = Math.Min(BaseDelaySeconds * Math.Pow(2, attempt - 1), MaxDelaySeconds);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private async Task EnsureInitializedAndConnected()
    {
        // Сначала пробуем токен из БД (TokenService): он обновляется через
        // refresh token и хранится в таблице TwitchToken. Если в БД есть
        // валидный токен с chat:read — используем его. Иначе — из конфигурации.
        try
        {
            if (_tokenService is null)
            {
                // Токен из БД недоступен (например, в тестах) — используем конфигурацию.
                if (string.IsNullOrWhiteSpace(_currentOAuth))
                {
                    _logger.LogWarning("Twitch OAuth не задан. Подключение к чату отложено.");
                    return;
                }

                _credentials = TryCreateCredentials(_currentOAuth);
                return;
            }

            var dbToken = await _tokenService.GetValidTokenAsync(CancellationToken.None);

            if (dbToken != null && !string.IsNullOrWhiteSpace(dbToken.AccessToken))
            {
                // Токен из БД валиден и имеет chat:read — используем его для IRC.
                // Бот подключается под именем владельца токена (rxdcodx).
                if (!string.Equals(_currentOAuth, dbToken.AccessToken, StringComparison.Ordinal))
                {
                    _logger.LogInformation(
                        "Используется токен из БД (владелец: rxdcodx) вместо конфигурационного"
                    );
                    _currentOAuth = dbToken.AccessToken;
                    _botName = "rxdcodx";
                    _credentials = TryCreateCredentials(_currentOAuth);
                }
            }
            else if (string.IsNullOrWhiteSpace(_currentOAuth))
            {
                _logger.LogWarning(
                    "Токен Twitch не найден ни в БД, ни в конфигурации (Twitch__OAuth). "
                        + "Подключение к чату отложено."
                );
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Не удалось получить токен из TokenService, используется конфигурация"
            );

            if (string.IsNullOrWhiteSpace(_currentOAuth))
            {
                return;
            }
        }

        var credentials = _credentials;

        if (credentials == null)
        {
            // Токена нет — подключаться нечем. Сервис продолжает работать,
            // подключение произойдёт, когда OAuth появится в конфигурации.
            _logger.LogWarning("Twitch chat connection skipped: OAuth token is not configured");
            return;
        }

        await _connectLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                _client.Initialize(credentials, TwitchConstants.Channel);
                _initialized = true;
            }

            if (!_client.IsConnected)
            {
                try
                {
                    await _client.ConnectAsync();
                }
                catch (InvalidOperationException ex)
                    when (ex.Message.Contains(
                            "already been started",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                {
                    _logger.LogWarning(
                        ex,
                        "Skipped duplicate Twitch connect attempt while WebSocket is already starting"
                    );
                }
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }
}
