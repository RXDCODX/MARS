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
    IDbContextFactory<MediaDbContext> dbFactory,
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
