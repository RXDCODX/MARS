using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.SoundRequest.Control;

namespace MARS.Platform.Infrastructure.Grpc.SoundRequest;

internal sealed class SoundRequestApi(GrpcClientFactory clientFactory) : ISoundRequestApi
{
    public async Task MuteAllAsync(CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .MuteAllAsync(new Empty(), cancellationToken: cancellationToken);
    }

    public async Task UnmuteAllAsync(CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .UnmuteAllAsync(new Empty(), cancellationToken: cancellationToken);
    }

    public Task<AudioQuizTrack> GetRandomTrackAsync(CancellationToken cancellationToken = default) =>
        clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .GetRandomTrackAsync(new Empty(), cancellationToken: cancellationToken)
            .ResponseAsync;
}