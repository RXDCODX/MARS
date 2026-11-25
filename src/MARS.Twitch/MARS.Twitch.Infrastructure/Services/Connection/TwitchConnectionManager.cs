using System.Threading;
using MARS.Twitch.Application.Configuration;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Api.Interfaces;
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
    private readonly ITwitchAPI? _api;

    private TwitchClient _client;
    private IClient _transport;
    private ConnectionCredentials? _credentials;
    private string _currentOAuth = string.Empty;
    private string _botName = string.Empty;
    private string _initializedWithToken = string.Empty;
    private readonly IDisposable? _changeToken;
    private bool _initialized;

    public bool IsConnected => _client.IsConnected;

    public string JoinedChannels =>
        string.Join(", ", _client.JoinedChannels.Select(c => c.Channel));

    public DateTime? LastConnectedAt { get; private set; }

    public DateTime? LastDisconnectedAt { get; private set; }

    public string? LastDisconnectReason { get; private set; }

    public int ReconnectAttempts { get; private set; }

    public bool IsReconnecting { get; private set; }

    private CancellationTokenSource? _reconnectCts;
    private Task? _reconnectTask;
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
        TokenService? tokenService = null,
        ITwitchAPI? api = null
    )
    {
        _logger = logger;
        _twitchOptions = twitchOptions;
        _loggerFactory = loggerFactory;
        _tokenService = tokenService;
        _api = api;

        var config = twitchOptions.CurrentValue;

        _transport = CreateTransportClient(config.TransportProtocol);
        _client = CreateTwitchClient(config.TransportProtocol);

        // Токен чата берётся только из БД (TokenService), куда он попадает через
        // OAuth-флоу сервиса. Конфигурационного фолбэка (Twitch__OAuth) больше нет.
        _currentOAuth = string.Empty;
        _botName = string.Empty;

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
            LastConnectedAt = DateTime.Now;
            ReconnectAttempts = 0;
            IsReconnecting = false;
            _logger.LogInformation("Twitch chat connected as {Bot}", _botName);
            return Task.CompletedTask;
        };

        _client.OnDisconnected += (_, _) =>
        {
            LastDisconnectedAt = DateTime.Now;
            _logger.LogWarning("Twitch chat disconnected");

            if (!_manualDisconnect && !IsReconnecting)
            {
                _ = TryReconnectAsync();
            }

            return Task.CompletedTask;
        };

        _client.OnConnectionError += (_, args) =>
        {
            LastDisconnectedAt = DateTime.Now;
            LastDisconnectReason = args.Error?.Message ?? "Unknown";
            _logger.LogError("Twitch connection error: {Message}", args.Error?.Message);

            if (!_manualDisconnect && !IsReconnecting)
            {
                _ = TryReconnectAsync();
            }

            return Task.CompletedTask;
        };

        // Транспорт (WebSocket/TCP) может получить обрыв от удалённой стороны без
        // штатного закрытия: TwitchLib при этом поднимает OnError, а не
        // OnDisconnected/OnFatality. Подписываемся напрямую, чтобы сразу запустить
        // переподключение, а не ждать ближайшей проверки здоровья (до 30 с).
        _transport.OnError += (_, args) =>
        {
            LastDisconnectedAt = DateTime.Now;
            LastDisconnectReason = args.Exception?.Message ?? "Unknown";
            _logger.LogError("Twitch transport error: {Message}", args.Exception?.Message);

            if (!_manualDisconnect && !IsReconnecting)
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

        // OAuth-токен чата теперь живёт только в БД (TokenService) и обновляется
        // через OAuth-флоу сервиса; изменения в конфигурации на него не влияют.
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
            + $"LastConnectedAt: {LastConnectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}\n"
            + $"LastDisconnectedAt: {LastDisconnectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}\n"
            + $"LastDisconnectReason: {LastDisconnectReason ?? "-"}\n"
            + $"ReconnectAttempts: {ReconnectAttempts}\n"
            + $"IsReconnecting: {IsReconnecting}";
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

            ReconnectAttempts = 0;
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
                IsReconnecting
                || _manualDisconnect
                || _reconnectCts?.Token.IsCancellationRequested == true
            )
            {
                return;
            }

            IsReconnecting = true;
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
            !cancellationToken.IsCancellationRequested && ReconnectAttempts < MaxReconnectAttempts
        )
        {
            ReconnectAttempts++;

            var delay = CalculateReconnectDelay(ReconnectAttempts);
            _logger.LogInformation(
                "Attempting to reconnect to Twitch (attempt {Attempt}/{Max}) in {Delay} seconds...",
                ReconnectAttempts,
                MaxReconnectAttempts,
                delay.TotalSeconds
            );

            try
            {
                await Task.Delay(delay, cancellationToken);

                if (_client.IsConnected)
                {
                    _logger.LogInformation("Already connected, stopping reconnect attempts");
                    IsReconnecting = false;
                    return;
                }

                await EnsureInitializedAndConnected();

                if (_client.IsConnected)
                {
                    _logger.LogInformation("Successfully reconnected to Twitch");
                    IsReconnecting = false;
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Reconnect cancelled");
                IsReconnecting = false;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnect attempt {Attempt} failed", ReconnectAttempts);
            }
        }

        if (ReconnectAttempts >= MaxReconnectAttempts)
        {
            _logger.LogError(
                "Failed to reconnect to Twitch after {MaxAttempts} attempts.",
                MaxReconnectAttempts
            );
        }

        IsReconnecting = false;
    }

    private static TimeSpan CalculateReconnectDelay(int attempt)
    {
        var delaySeconds = Math.Min(BaseDelaySeconds * Math.Pow(2, attempt - 1), MaxDelaySeconds);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private async Task EnsureInitializedAndConnected()
    {
        // Токен чата берётся только из БД (TokenService), куда он попадает
        // через OAuth-флоу сервиса. Конфигурационного фолбэка больше нет.
        string accessToken;
        try
        {
            if (_tokenService is null)
            {
                _logger.LogWarning("TokenService недоступен — подключение к чату отложено.");
                return;
            }

            var dbToken = await _tokenService.GetValidTokenAsync(CancellationToken.None);

            if (dbToken is null || string.IsNullOrWhiteSpace(dbToken.AccessToken))
            {
                _logger.LogWarning(
                    "Токен Twitch не найден в БД. Пройдите авторизацию (кнопка на /twitch-auth "
                        + "или ссылка из Telegram). Подключение к чату отложено."
                );
                return;
            }

            accessToken = dbToken.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить токен из TokenService");
            return;
        }

        if (
            _initialized
            && string.Equals(_initializedWithToken, accessToken, StringComparison.Ordinal)
            && _client.IsConnected
        )
        {
            return;
        }

        var botLogin = await ResolveBotLoginAsync(accessToken);
        if (string.IsNullOrWhiteSpace(botLogin))
        {
            _logger.LogWarning(
                "Не удалось определить логин владельца токена (GET /users). "
                    + "Подключение к чату отложено."
            );
            return;
        }

        _botName = botLogin;
        _currentOAuth = accessToken;
        _credentials = TryCreateCredentials(_currentOAuth);

        if (_credentials is null)
        {
            return;
        }

        await _connectLock.WaitAsync();
        try
        {
            if (_client.IsConnected)
            {
                try
                {
                    await _client.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отключить старый клиент Twitch");
                }
            }

            var config = _twitchOptions.CurrentValue;
            _transport = CreateTransportClient(config.TransportProtocol);
            _client = CreateTwitchClient(config.TransportProtocol);
            _initialized = false;
            WireEvents();

            _client.Initialize(_credentials, TwitchConstants.Channel);
            _initialized = true;
            _initializedWithToken = accessToken;

            if (!_client.IsConnected)
            {
                try
                {
                    await _client.ConnectAsync();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Failed to connect to Twitch");
                }
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task<string?> ResolveBotLoginAsync(string accessToken)
    {
        if (_api is null)
        {
            return null;
        }

        try
        {
            _api.Settings.AccessToken = accessToken;
            var response = await _api.Helix.Users.GetUsersAsync();
            return response.Users.FirstOrDefault()?.Login;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить логин владельца токена из Helix");
            return null;
        }
    }
}
