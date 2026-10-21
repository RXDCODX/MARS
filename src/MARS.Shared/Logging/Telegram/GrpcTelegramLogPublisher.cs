using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MARS.Shared.Grpc.TelegramLog;

namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Доставка записи журнала в Telegram по gRPC к <c>MARS.Telegram</c>.
/// </summary>
/// <remarks>
/// <para>
/// Канал создаётся отправителем, а не берётся из контейнера зависимостей:
/// отправитель живёт внутри Serilog, настройка которого выполняется до сборки
/// контейнера, и поднимать контейнер ради одного клиента — значит создать
/// второй набор одиночных экземпляров и потерять их утилизацию.
/// </para>
/// <para>
/// Ошибки стандартные: <c>FAILED_PRECONDITION</c> означает, что Telegram не
/// настроен принимать журнал, и повторять доставку бессмысленно.
/// </para>
/// </remarks>
public sealed class GrpcTelegramLogPublisher : ITelegramLogPublisher
{
    private readonly GrpcChannel _channel;
    private readonly TelegramLogService.TelegramLogServiceClient _client;

    /// <summary>
    /// Создаёт отправитель.
    /// </summary>
    /// <param name="address">Адрес gRPC-эндпоинта MARS.Telegram.</param>
    public GrpcTelegramLogPublisher(Uri address)
    {
        _channel = GrpcChannel.ForAddress(address);
        _client = new TelegramLogService.TelegramLogServiceClient(_channel);
    }

    /// <inheritdoc />
    public async Task<TelegramLogPublishResult> PublishAsync(
        TelegramLogEntry entry,
        CancellationToken cancellationToken
    )
    {
        var result = new TelegramLogPublishResult();

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
            var reply = await _client.PublishAsync(request, cancellationToken: cancellationToken);

            result.Recipients = reply.Recipients;
            result.Suppressed = reply.Suppressed;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new TelegramLogNotConfiguredException(exception.Status.Detail);
        }

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _channel.Dispose();
    }
}
