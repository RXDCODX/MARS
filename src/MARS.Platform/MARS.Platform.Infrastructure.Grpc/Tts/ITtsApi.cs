using MARS.Shared.Contracts.Tts.Control;
using Google.Protobuf.WellKnownTypes;

namespace MARS.Platform.Infrastructure.Grpc.Tts;

public interface ITtsApi
{
    Task SetMuteStateAsync(bool isMuted, double volume, CancellationToken cancellationToken = default);
}