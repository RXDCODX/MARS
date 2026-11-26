using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Tts.Control;

namespace MARS.Platform.Infrastructure.Grpc.Tts;

internal sealed class TtsApi(GrpcClientFactory clientFactory) : ITtsApi
{
    public async Task SetMuteStateAsync(bool isMuted, double volume, CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .SetMuteStateAsync(
                new TtsMuteStateRequest { IsMuted = isMuted, Volume = volume },
                cancellationToken: cancellationToken
            );
    }
}