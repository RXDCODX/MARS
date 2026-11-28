using System.Diagnostics.CodeAnalysis;
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

    Task SetFilterEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    Task<bool> ToggleFilterAsync(CancellationToken cancellationToken = default);

    Task ReassignVoiceAsync(string userId, CancellationToken cancellationToken = default);

    Task<double> GetVolumeAsync(CancellationToken cancellationToken = default);

    Task SetVolumeAsync(double volume, CancellationToken cancellationToken = default);
}
