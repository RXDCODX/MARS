using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using MARS.TwitchCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace MARS.TwitchCore.Services.EventSub;

public sealed class EventSubRawForwarder(
    EventSubWebsocketClient wsClient,
    IMarsEventBus eventBus,
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<EventSubRawForwarder> logger
) : IHostedService, IAsyncDisposable
{
    private const string ExchangeName = RabbitMqConfig.ExchangeName;
    private const string RoutingKey = RabbitMqConfig.TwitchEventSubRaw;

    private readonly Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(
        new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    private EventInfo? _onDataReceivedEvent;
    private object? _websocketClient;
    private EventHandler? _handler;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;
    private bool _subscribed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token), _cts.Token);
        TrySubscribe();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        TryUnsubscribe();
        if (_cts != null)
        {
            try
            {
                _cts.Cancel();
            }
            catch
            {
                // ignore
            }
        }
        _channel.Writer.TryComplete();

        if (_workerTask != null)
        {
            try
            {
                await _workerTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch
            {
                // ignore
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        TryUnsubscribe();
        if (_cts != null)
        {
            _cts.Dispose();
            _cts = null;
        }
        _channel.Writer.TryComplete();
        if (_workerTask != null)
        {
            try
            {
                await _workerTask;
            }
            catch
            {
                // ignore
            }
            _workerTask = null;
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed)
        {
            return;
        }

        try
        {
            var wsClientType = wsClient.GetType();
            var field = wsClientType.GetField(
                "_websocketClient",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (field == null)
            {
                logger.LogWarning(
                    "EventSubRawForwarder: не удалось найти поле _websocketClient у {Type}",
                    wsClientType.FullName
                );
                return;
            }

            _websocketClient = field.GetValue(wsClient);
            if (_websocketClient == null)
            {
                logger.LogWarning("EventSubRawForwarder: _websocketClient is null");
                return;
            }

            var wsType = _websocketClient.GetType();
            _onDataReceivedEvent = wsType.GetEvent(
                "OnDataReceived",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (_onDataReceivedEvent == null)
            {
                logger.LogWarning(
                    "EventSubRawForwarder: не удалось найти событие OnDataReceived у {Type}",
                    wsType.FullName
                );
                return;
            }

            _handler = OnDataReceivedHandler;
            _onDataReceivedEvent.AddEventHandler(_websocketClient, _handler);
            _subscribed = true;
            logger.LogInformation("EventSubRawForwarder: подписка на сырой фрейм установлена");
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogError(ex, "EventSubRawForwarder: ошибка при подписке на сырой фрейм");
        }
    }

    private void TryUnsubscribe()
    {
        if (
            !_subscribed
            || _websocketClient == null
            || _onDataReceivedEvent == null
            || _handler == null
        )
        {
            return;
        }

        try
        {
            _onDataReceivedEvent.RemoveEventHandler(_websocketClient, _handler);
        }
        catch
        {
            // ignore
        }

        _subscribed = false;
    }

    private void OnDataReceivedHandler(object? sender, EventArgs e)
    {
        try
        {
            if (e == null)
            {
                return;
            }

            var dataArgsType = e.GetType();
            var bytesProp = dataArgsType.GetProperty(
                "Bytes",
                BindingFlags.Instance | BindingFlags.Public
            );
            if (bytesProp == null)
            {
                return;
            }

            if (bytesProp.GetValue(e) is not byte[] bytes || bytes.Length == 0)
            {
                return;
            }

            MarsMetrics.EventSubRawForwarded.Add(1);

            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);

            if (!_channel.Writer.TryWrite(copy))
            {
                MarsMetrics.EventSubRawDropped.Add(1);
            }
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogDebug(ex, "EventSubRawForwarder: ошибка в обработчике сырых фреймов");
        }
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var bytes in _channel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    await PublishRawFrameAsync(bytes, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MarsMetrics.EventSubRawErrors.Add(1);
                    logger.LogDebug(ex, "EventSubRawForwarder: ошибка публикации сырых фреймов");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // graceful
        }
        catch (Exception ex)
        {
            MarsMetrics.EventSubRawErrors.Add(1);
            logger.LogError(ex, "EventSubRawForwarder: фоновый воркер завершился с ошибкой");
        }
    }

    private async Task PublishRawFrameAsync(byte[] bytes, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;

        string messageType = "unknown";
        if (root.TryGetProperty("metadata", out var metadataEl))
        {
            if (metadataEl.TryGetProperty("message_type", out var mt))
            {
                messageType = mt.GetString() ?? "unknown";
            }
        }

        string? subscriptionType = null;
        string? subscriptionVersion = null;
        string? messageId = null;

        if (root.TryGetProperty("metadata", out var metadataEl2))
        {
            if (metadataEl2.TryGetProperty("message_id", out var mid))
            {
                messageId = mid.GetString();
            }
            if (metadataEl2.TryGetProperty("subscription_type", out var st))
            {
                subscriptionType = st.GetString();
            }
            if (metadataEl2.TryGetProperty("subscription_version", out var sv))
            {
                subscriptionVersion = sv.GetString();
            }
        }

        string? subscriptionId = null;
        if (root.TryGetProperty("payload", out var payloadEl))
        {
            if (
                payloadEl.TryGetProperty("subscription", out var subEl)
                && subEl.ValueKind == JsonValueKind.Object
                && subEl.TryGetProperty("id", out var sid)
            )
            {
                subscriptionId = sid.GetString();
            }
        }

        var envelope = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["messageId"] = messageId,
            ["messageType"] = messageType,
            ["subscriptionType"] = subscriptionType,
            ["subscriptionVersion"] = subscriptionVersion,
            ["subscriptionId"] = subscriptionId,
            ["sessionId"] = wsClient.SessionId,
            ["eventName"] = subscriptionType ?? messageType,
            ["observedAtUtc"] = DateTimeOffset.UtcNow,
            ["sourceService"] = "MARS.TwitchCore",
            ["traceParent"] = Activity.Current?.Id,
            ["raw"] = JsonSerializer.Deserialize<JsonElement>(bytes),
        };

        var rewardMetadata = await ReadRewardMetadataAsync(root, subscriptionType, ct);
        if (rewardMetadata is not null)
        {
            // Служебные настройки MARS передаются рядом с raw, но исходный Twitch
            // JSON остаётся неизменным и единственным источником события.
            envelope["rewardMetadata"] = rewardMetadata;
        }

        await eventBus.PublishAsync(RoutingKey, envelope, ct);
        MarsMetrics.EventSubRawPublished.Add(1);
    }

    private async Task<object?> ReadRewardMetadataAsync(
        JsonElement root,
        string? subscriptionType,
        CancellationToken cancellationToken
    )
    {
        const string redemptionType = "channel.channel_points_custom_reward_redemption.add";

        if (!string.Equals(subscriptionType, redemptionType, StringComparison.Ordinal))
        {
            return null;
        }

        string? rewardId = null;

        try
        {
            if (
                !TryGetProperty(root, out var payload, "payload")
                || !TryGetProperty(payload, out var eventElement, "event")
                || !TryGetProperty(eventElement, out var reward, "reward")
                || !TryGetProperty(reward, out var rewardIdElement, "id")
                || rewardIdElement.ValueKind != JsonValueKind.String
            )
            {
                return null;
            }

            rewardId = rewardIdElement.GetString();
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return null;
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var record = await db
                .ChannelRewards.AsNoTracking()
                .Where(item => item.TwitchRewardId == rewardId && !item.IsDeleted)
                .Select(item => new { item.AlertKey, item.MediaInfoId })
                .FirstOrDefaultAsync(cancellationToken);

            return record is null
                ? null
                : new { alertKey = record.AlertKey, mediaInfoId = record.MediaInfoId };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Каталог MARS обогащает специальные эффекты и медиа, но не является
            // условием доставки EventSub: при сбое отправляем исходный JSON.
            logger.LogWarning(
                exception,
                "Не удалось получить настройки MARS для Twitch reward {RewardId}; raw EventSub будет отправлен без обогащения",
                rewardId
            );
            return null;
        }
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement value, string name)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
