using Google.Protobuf.WellKnownTypes;
using MARS.Shared.Contracts.SoundRequest.Control;

namespace MARS.Platform.Infrastructure.Grpc.SoundRequest;

public interface ISoundRequestApi
{
    Task MuteAllAsync(CancellationToken cancellationToken = default);

    Task UnmuteAllAsync(CancellationToken cancellationToken = default);

    Task<AudioQuizTrack> GetRandomTrackAsync(CancellationToken cancellationToken = default);
}
