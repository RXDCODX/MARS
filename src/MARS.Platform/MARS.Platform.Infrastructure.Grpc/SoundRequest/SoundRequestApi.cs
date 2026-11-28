using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.SoundRequest.Control;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.Grpc.SoundRequest;

[ExcludeFromCodeCoverage]
internal sealed class SoundRequestApi(GrpcClientFactory clientFactory) : ISoundRequestApi
{
    public async Task MuteAllAsync(CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>(
                "SoundRequest"
            )
            .MuteAllAsync(new Empty(), cancellationToken: cancellationToken);
    }

    public async Task UnmuteAllAsync(CancellationToken cancellationToken = default)
    {
        await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>(
                "SoundRequest"
            )
            .UnmuteAllAsync(new Empty(), cancellationToken: cancellationToken);
    }

    public Task<AudioQuizTrack> GetRandomTrackAsync(
        CancellationToken cancellationToken = default
    ) =>
        clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>(
                "SoundRequest"
            )
            .GetRandomTrackAsync(new Empty(), cancellationToken: cancellationToken)
            .ResponseAsync;

    public async Task<string> GetUserQueuePositionAsync(string twitchId, CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .GetUserQueuePositionAsync(new UserRequest { TwitchId = twitchId }, cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> GetCurrentSongAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .GetCurrentSongAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> PausePlaybackAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .PausePlaybackAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> ResumePlaybackAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .ResumePlaybackAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> StopPlaybackAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .StopPlaybackAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> AddTrackAsync(string query, string requestedByTwitchId, CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .AddTrackAsync(new AddTrackRequest { Query = query, RequestedByTwitchId = requestedByTwitchId }, cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> AddPlaylistAsync(string playlistUrl, string requestedByTwitchId, int maxTracks, CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .AddPlaylistAsync(new AddPlaylistRequest { PlaylistUrl = playlistUrl, RequestedByTwitchId = requestedByTwitchId, MaxTracks = maxTracks }, cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> ClearQueueAsync(CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .ClearQueueAsync(new Empty(), cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> CancelLastTrackAsync(string twitchId, CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .CancelLastTrackAsync(new CancelLastTrackRequest { TwitchId = twitchId }, cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> SetVolumeAsync(int? volume, CancellationToken cancellationToken = default)
    {
        var request = new SetVolumeRequest();
        if (volume.HasValue)
        {
            request.Volume = volume.Value;
        }

        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .SetVolumeAsync(request, cancellationToken: cancellationToken);
        return response.Text;
    }

    public async Task<string> StartSpotifyAuthorizationAsync(string redirectUri, CancellationToken cancellationToken = default)
    {
        var response = await clientFactory
            .CreateClient<SoundRequestControlService.SoundRequestControlServiceClient>("SoundRequest")
            .StartSpotifyAuthorizationAsync(new SpotifyAuthStartRequest { RedirectUri = redirectUri }, cancellationToken: cancellationToken);
        return response.Text;
    }
}
