using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Shared.Contracts.SoundRequest.Control;
using MARS.SoundRequest.Application.Models;
using MARS.SoundRequest.Domain.Entities;
using MARS.SoundRequest.Infrastructure.Data;
using MARS.SoundRequest.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.SoundRequest.Api.Grpc;

public sealed class SoundRequestControlGrpcService(
    StateManager stateManager,
    MainPlayer mainPlayer,
    IDbContextFactory<MediaDbContext> dbFactory,
    SoundRequestCommandsService commandsService,
    SpotifyAuthService spotifyAuthService,
    ILogger<SoundRequestControlGrpcService> logger
) : SoundRequestControlService.SoundRequestControlServiceBase
{
    public override Task<Empty> MuteAll(Empty request, ServerCallContext context)
    {
        var state = stateManager.GetState();
        if (state.State == PlaybackState.Playing)
        {
            stateManager.SetPausedAsync(true).GetAwaiter().GetResult();
            stateManager.SetPausedByMuteAsync(true).GetAwaiter().GetResult();
        }

        stateManager.SetMutedAsync(true).GetAwaiter().GetResult();
        logger.LogInformation("Плеер заглушён координатором глушения звука");
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> UnmuteAll(Empty request, ServerCallContext context)
    {
        stateManager.SetMutedAsync(false).GetAwaiter().GetResult();
        var state = stateManager.GetState();
        if (state.PausedByMute)
        {
            stateManager.SetPausedAsync(false).GetAwaiter().GetResult();
            stateManager.SetPausedByMuteAsync(false).GetAwaiter().GetResult();
        }

        logger.LogInformation("Mute снят с плеера координатором глушения звука");
        return Task.FromResult(new Empty());
    }

    public override async Task<AudioQuizTrack> GetRandomTrack(
        Empty request,
        ServerCallContext context
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
            var track = await db
                .Tracks.AsNoTracking()
                .Where(t => !t.IsDeleted)
                .OrderBy(_ => EF.Functions.Random())
                .FirstOrDefaultAsync(context.CancellationToken);

            if (track is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Трек не найден"));
            }

            return new AudioQuizTrack
            {
                Url = track.Url.AbsoluteUri,
                ArtworkUrl = track.ArtworkUrl?.AbsoluteUri ?? string.Empty,
                Title = track.Title,
                DurationSeconds = (int)track.Duration.TotalSeconds,
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get random track");
            throw new RpcException(new Status(StatusCode.Internal, "Не удалось получить трек"));
        }
        }
    }

    public override async Task<StringResult> GetUserQueuePosition(
        UserRequest request,
        ServerCallContext context
    )
    {
        var text = await commandsService.GetUserQueuePositionAsync(request.TwitchId ?? string.Empty);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> GetCurrentSong(
        Empty request,
        ServerCallContext context
    )
    {
        var text = await commandsService.GetCurrentSongAsync(context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> PausePlayback(
        Empty request,
        ServerCallContext context
    )
    {
        var text = await commandsService.PausePlaybackAsync(context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> ResumePlayback(
        Empty request,
        ServerCallContext context
    )
    {
        var text = await commandsService.ResumePlaybackAsync(context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> StopPlayback(
        Empty request,
        ServerCallContext context
    )
    {
        var text = await commandsService.StopPlaybackAsync(context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> StartSpotifyAuthorization(
        SpotifyAuthStartRequest request,
        ServerCallContext context
    )
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        var clientId = await db.RootStates
            .AsNoTracking()
            .Where(x => x.Name == MARS.SoundRequest.Domain.Entities.RootStateKeys.SoundRequestSpotifyClientId)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(context.CancellationToken);
        var clientSecret = await db.RootStates
            .AsNoTracking()
            .Where(x => x.Name == MARS.SoundRequest.Domain.Entities.RootStateKeys.SoundRequestSpotifyClientSecret)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return new StringResult { Text = "Нужны ClientId и ClientSecret", Success = false };
        }

        var redirectUri = string.IsNullOrWhiteSpace(request?.RedirectUri)
            ? "http://localhost/callback"
            : request.RedirectUri;
        var result = await spotifyAuthService.StartAuthorizationAsync(clientId, clientSecret, redirectUri, context.CancellationToken);
        return new StringResult
        {
            Text = result.Success ? result.AuthUrl : result.Message,
            Success = result.Success,
        };
    }

    public override async Task<StringResult> AddTrack(
        AddTrackRequest request,
        ServerCallContext context
    )
    {
        var text = await commandsService.AddTrackAsync(request.Query ?? string.Empty, request.RequestedByTwitchId ?? string.Empty, context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> AddPlaylist(
        AddPlaylistRequest request,
        ServerCallContext context
    )
    {
        var text = await commandsService.AddPlaylistAsync(request.PlaylistUrl ?? string.Empty, request.RequestedByTwitchId ?? string.Empty, request.MaxTracks <= 0 ? 10 : request.MaxTracks, context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> ClearQueue(
        Empty request,
        ServerCallContext context
    )
    {
        var text = await commandsService.ClearQueueAsync(context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> CancelLastTrack(
        CancelLastTrackRequest request,
        ServerCallContext context
    )
    {
        var text = await commandsService.CancelLastTrackAsync(request.TwitchId ?? string.Empty, context.CancellationToken);
        return new StringResult { Text = text, Success = true };
    }

    public override async Task<StringResult> SetVolume(
        SetVolumeRequest request,
        ServerCallContext context
    )
    {
        if (request.HasVolume)
        {
            var volume = Math.Clamp(request.Volume, 0, 100);
            await mainPlayer.SetVolumeAsync(volume, context.CancellationToken);
            return new StringResult { Text = $"Громкость установлена: {volume}", Success = true };
        }

        var state = mainPlayer.GetState();
        return new StringResult { Text = $"Текущая громкость: {state.Volume}", Success = true };
    }
}
