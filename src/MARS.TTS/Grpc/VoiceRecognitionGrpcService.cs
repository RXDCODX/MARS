using Grpc.Core;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Voice;

namespace MARS.TTS.Grpc;

/// <summary>
/// gRPC-сервис доставки TTS потребителям (AudioController): потребитель
/// подписывается одним server-streaming-вызовом и отчитывается о
/// воспроизведении unary-вызовами.
/// </summary>
public sealed class VoiceRecognitionGrpcService(
    GrpcEventBroadcaster<VoiceEvent> broadcaster,
    ILogger<VoiceRecognitionGrpcService> logger
) : VoiceRecognitionService.VoiceRecognitionServiceBase
{
    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<VoiceEvent> responseStream,
        ServerCallContext context
    )
    {
        using var subscription = broadcaster.Subscribe();

        logger.LogInformation("TTS consumer subscribed: {SubscriberId}", subscription.Id);

        await broadcaster.PumpAsync(subscription, responseStream, context.CancellationToken);
    }

    public override Task<ReportTtsPlaybackStartedResponse> ReportTtsPlaybackStarted(
        ReportTtsPlaybackStartedRequest request,
        ServerCallContext context
    )
    {
        logger.LogInformation(
            "TTS playback started by consumer {Peer}: {Text}",
            context.Peer,
            request.Text
        );

        return Task.FromResult(new ReportTtsPlaybackStartedResponse());
    }

    public override Task<ReportTtsPlaybackCompletedResponse> ReportTtsPlaybackCompleted(
        ReportTtsPlaybackCompletedRequest request,
        ServerCallContext context
    )
    {
        var duration = TimeSpan.FromSeconds(request.DurationSeconds);

        logger.LogInformation(
            "TTS playback completed by consumer {Peer}: {Text}, duration={Duration}",
            context.Peer,
            request.Text,
            duration
        );

        return Task.FromResult(new ReportTtsPlaybackCompletedResponse());
    }

    public override Task<ReportTtsPlaybackFailedResponse> ReportTtsPlaybackFailed(
        ReportTtsPlaybackFailedRequest request,
        ServerCallContext context
    )
    {
        logger.LogWarning(
            "TTS playback failed on consumer {Peer}: {Text}, error={Error}",
            context.Peer,
            request.Text,
            request.Error
        );

        return Task.FromResult(new ReportTtsPlaybackFailedResponse());
    }
}
