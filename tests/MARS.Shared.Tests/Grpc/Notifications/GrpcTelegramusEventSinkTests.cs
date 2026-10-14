using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using TelegramusServiceClient = MARS.Shared.Grpc.Telegramus.TelegramusService.TelegramusServiceClient;

namespace MARS.Shared.Tests.Grpc.Notifications;

/// <summary>
/// Доставка события оверлея из чужого процесса через unary-вызов <c>Fire</c>:
/// успех уходит получателю, а недоступность владельца оверлея гасится, чтобы не
/// ронять консьюмера, для которого событие — побочный эффект.
/// </summary>
public class GrpcTelegramusEventSinkTests
{
    [Fact]
    public async Task DispatchForwardsEventToAlerts()
    {
        var client = new Mock<TelegramusServiceClient>(Mock.Of<CallInvoker>());
        client
            .Setup(c =>
                c.FireAsync(
                    It.IsAny<FireRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Completed());
        var sink = new GrpcTelegramusEventSink(
            client.Object,
            NullLogger<GrpcTelegramusEventSink>.Instance
        );
        var notification = new TelegramusEvent { Explosion = new EmptyEvent() };

        await sink.DispatchAsync(notification);

        client.Verify(
            c =>
                c.FireAsync(
                    It.Is<FireRequest>(request => ReferenceEquals(request.Event, notification)),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeliveryFailureIsSwallowedAndLogged()
    {
        var client = new Mock<TelegramusServiceClient>(Mock.Of<CallInvoker>());
        client
            .Setup(c =>
                c.FireAsync(
                    It.IsAny<FireRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "alerts недоступен")));
        var sink = new GrpcTelegramusEventSink(
            client.Object,
            NullLogger<GrpcTelegramusEventSink>.Instance
        );

        await sink.DispatchAsync(new TelegramusEvent { Explosion = new EmptyEvent() });

        client.Verify(
            c =>
                c.FireAsync(
                    It.IsAny<FireRequest>(),
                    It.IsAny<Metadata>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    private static AsyncUnaryCall<FireResponse> Completed() =>
        new(
            Task.FromResult(new FireResponse()),
            Task.FromResult(new Metadata()),
            () => new Status(),
            () => new Metadata(),
            () => { }
        );
}
