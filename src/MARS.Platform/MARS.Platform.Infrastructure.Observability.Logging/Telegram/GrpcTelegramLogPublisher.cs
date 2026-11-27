using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.TelegramLog;

namespace MARS.Platform.Infrastructure.Observability.Logging.Telegram;

/// <summary>
/// Доставка записи журнала в MARS.Telegram по gRPC. Канал создаётся и
/// освобождается самим отправителем, потому что Serilog-синк настраивается до
/// сборки контейнера зависимостей.
/// </summary>
public sealed class GrpcTelegramLogPublisher : ITelegramLogPublisher
{
    private readonly Func<LogEntryRequest, CancellationToken, Task<LogEntryReply>> _publish;
    private readonly GrpcChannel? _channel;

    public GrpcTelegramLogPublisher(
        Uri address,
        string? apiKey = null,
        string? apiKeyHeaderName = null
    )
    {
        _channel = GrpcChannel.ForAddress(
            address,
            new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                },
            }
        );

        var client = new TelegramLogService.TelegramLogServiceClient(_channel.CreateCallInvoker());
        _publish = (request, cancellationToken) =>
            client
                .PublishAsync(
                    request,
                    CreateCallOptions(
                        apiKey,
                        apiKeyHeaderName ?? ServiceAuthOptions.DefaultApiKeyHeaderName,
                        cancellationToken
                    )
                )
                .ResponseAsync;
    }

    internal GrpcTelegramLogPublisher(
        Func<LogEntryRequest, CancellationToken, Task<LogEntryReply>> publish
    )
    {
        _publish = publish;
    }

    /// <inheritdoc />
    public async Task<TelegramLogPublishResult> PublishAsync(
        TelegramLogEntry entry,
        CancellationToken cancellationToken
    )
    {
        var request = new LogEntryRequest
        {
            Service = entry.Service,
            Level = entry.Level,
            Category = entry.Category,
            Message = entry.Message,
            Exception = entry.Exception,
            Timestamp = Timestamp.FromDateTimeOffset(entry.Timestamp),
            SuppressedCount = entry.SuppressedCount,
        };

        try
        {
            var reply = await _publish(request, cancellationToken);
            return new TelegramLogPublishResult
            {
                Recipients = reply.Recipients,
                Suppressed = reply.Suppressed,
            };
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new TelegramLogNotConfiguredException(exception.Status.Detail);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _channel?.Dispose();

    private CallOptions CreateCallOptions(
        string? apiKey,
        string apiKeyHeaderName,
        CancellationToken cancellationToken
    )
    {
        Metadata? headers = null;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            headers = new Metadata { { apiKeyHeaderName.ToLowerInvariant(), apiKey } };
        }

        return new CallOptions(headers: headers, cancellationToken: cancellationToken);
    }
}
