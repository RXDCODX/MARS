using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Tts.Control;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.Grpc.Tts;

[ExcludeFromCodeCoverage]
internal sealed class TtsApi(GrpcClientFactory clientFactory) : ITtsApi
{
    public async Task SetMuteStateAsync(
        bool isMuted,
        double volume,
        CancellationToken cancellationToken = default
    )
    {
        await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .SetMuteStateAsync(
                new TtsMuteStateRequest { IsMuted = isMuted, Volume = volume },
                cancellationToken: cancellationToken
            );
    }

    public async Task SetFilterEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .SetFilterEnabledAsync(new TtsFilterRequest { Enabled = enabled }, cancellationToken: cancellationToken);
    }

    public async Task<bool> ToggleFilterAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .ToggleFilterAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Success;
    }

    public async Task ReassignVoiceAsync(string userId, CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .ReassignVoiceAsync(new TtsReassignVoiceRequest { UserId = userId }, cancellationToken: cancellationToken);
    }

    public async Task<double> GetVolumeAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .GetVolumeAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Volume;
    }

    public async Task SetVolumeAsync(double volume, CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<TtsControlService.TtsControlServiceClient>("Tts")
            .SetVolumeAsync(new TtsVolumeRequest { Volume = volume }, cancellationToken: cancellationToken);
    }
}
