using Google.Protobuf.WellKnownTypes;
using MARS.Shared.Contracts.Tts.Control;

namespace MARS.Platform.Infrastructure.Grpc.Tts;

public interface ITtsApi
{
    Task SetMuteStateAsync(
        bool isMuted,
        double volume,
        CancellationToken cancellationToken = default
    );
}
