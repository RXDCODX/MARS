using System.Diagnostics.CodeAnalysis;
using Google.Protobuf.WellKnownTypes;
using MARS.Shared.Contracts.SoundRequest.Control;

namespace MARS.Platform.Infrastructure.Grpc.SoundRequest;

public interface ISoundRequestApi
{
    Task MuteAllAsync(CancellationToken cancellationToken = default);

    Task UnmuteAllAsync(CancellationToken cancellationToken = default);

    Task<AudioQuizTrack> GetRandomTrackAsync(CancellationToken cancellationToken = default);

    Task<string> GetUserQueuePositionAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    );

    Task<string> GetCurrentSongAsync(CancellationToken cancellationToken = default);

    Task<string> PausePlaybackAsync(CancellationToken cancellationToken = default);

    Task<string> ResumePlaybackAsync(CancellationToken cancellationToken = default);

    Task<string> StopPlaybackAsync(CancellationToken cancellationToken = default);

    Task<string> AddTrackAsync(
        string query,
        string requestedByTwitchId,
        CancellationToken cancellationToken = default
    );

    Task<string> AddPlaylistAsync(
        string playlistUrl,
        string requestedByTwitchId,
        int maxTracks,
        CancellationToken cancellationToken = default
    );

    Task<string> ClearQueueAsync(CancellationToken cancellationToken = default);

    Task<string> CancelLastTrackAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    );

    Task<string> SetVolumeAsync(int? volume, CancellationToken cancellationToken = default);

    Task<string> StartSpotifyAuthorizationAsync(
        string redirectUri,
        CancellationToken cancellationToken = default
    );
}
