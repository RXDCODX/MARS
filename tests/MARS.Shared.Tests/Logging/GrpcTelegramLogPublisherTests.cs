using Grpc.Core;
using MARS.Platform.Infrastructure.Observability.Logging.Telegram;
using MARS.Shared.Contracts.TelegramLog;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Отправитель журнала преобразует запись в typed Protobuf и различает постоянную
/// ошибку конфигурации от временного gRPC-отказа. Сеть не используется.
/// </summary>
public sealed class GrpcTelegramLogPublisherTests
{
    [Fact]
    public async Task EntryIsMappedToAllProtoFields()
    {
        LogEntryRequest? sent = null;
        using var publisher = new GrpcTelegramLogPublisher(
            (request, cancellationToken) =>
            {
                sent = request;
                Assert.Equal(TestContext.Current.CancellationToken, cancellationToken);
                return Task.FromResult(new LogEntryReply { Recipients = 2, Suppressed = 1 });
            }
        );
        var timestamp = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        var entry = new TelegramLogEntry
        {
            Service = "MARS.Shared.Tests",
            Level = "Error",
            Category = "MARS.Shared.Tests.Logging",
            Message = "Что-то сломалось",
            Exception = "System.InvalidOperationException: причина",
            Timestamp = timestamp,
            SuppressedCount = 3,
        };

        var result = await publisher.PublishAsync(entry, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Recipients);
        Assert.Equal(1, result.Suppressed);
        Assert.NotNull(sent);
        Assert.Equal(entry.Service, sent!.Service);
        Assert.Equal(entry.Level, sent.Level);
        Assert.Equal(entry.Category, sent.Category);
        Assert.Equal(entry.Message, sent.Message);
        Assert.Equal(entry.Exception, sent.Exception);
        Assert.Equal(3, sent.SuppressedCount);
        Assert.Equal(timestamp, sent.Timestamp.ToDateTimeOffset());
    }

    [Fact]
    public async Task FailedPreconditionDisablesDelivery()
    {
        using var publisher = new GrpcTelegramLogPublisher(
            (_, _) =>
                throw new RpcException(
                    new Status(StatusCode.FailedPrecondition, "Telegram не настроен")
                )
        );

        var error = await Assert.ThrowsAsync<TelegramLogNotConfiguredException>(() =>
            publisher.PublishAsync(Entry(), TestContext.Current.CancellationToken)
        );

        Assert.Contains("не настроен", error.Message);
    }

    [Fact]
    public async Task TemporaryFailureRemainsRetryable()
    {
        var failure = new RpcException(new Status(StatusCode.Unavailable, "unavailable"));
        using var publisher = new GrpcTelegramLogPublisher((_, _) => throw failure);

        var actual = await Assert.ThrowsAsync<RpcException>(() =>
            publisher.PublishAsync(Entry(), TestContext.Current.CancellationToken)
        );

        Assert.Same(failure, actual);
    }

    private static TelegramLogEntry Entry() =>
        new()
        {
            Service = "MARS.Shared.Tests",
            Level = "Error",
            Message = "Что-то сломалось",
        };
}
