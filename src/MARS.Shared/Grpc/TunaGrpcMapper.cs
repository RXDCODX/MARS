using MARS.Shared.Grpc.Models;
using MARS.Shared.Grpc.Tuna;

namespace MARS.Shared.Grpc;

public static class TunaGrpcMapper
{
    public static TunaPayload ToProto(TunaMusicDTO info)
    {
        return new TunaPayload
        {
            Data = ToProto(info.Data),
            Hostname = info.Hostname ?? string.Empty,
            Timestamp = info.Date ?? string.Empty,
        };
    }

    public static TunaMusicDTO ToDto(TunaPayload payload)
    {
        return new TunaMusicDTO
        {
            Data = ToDto(payload.Data),
            Hostname = payload.Hostname,
            Date = payload.Timestamp,
        };
    }

    private static TunaTrack ToProto(TunaMusicData data)
    {
        var track = new TunaTrack
        {
            Id = data.Id.ToString(),
            Cover = data.Cover ?? string.Empty,
            Title = data.Title ?? string.Empty,
            Status = data.Status ?? string.Empty,
            Progression = data.Progression,
            Duration = data.Duration,
            AlbumUrl = data.AlbumUrl ?? string.Empty,
        };

        track.Artists.AddRange(data.Artists ?? []);

        return track;
    }

    private static TunaMusicData ToDto(TunaTrack track)
    {
        return new TunaMusicData
        {
            Id = MediaGrpcMapper.ParseGuid(track.Id),
            Cover = track.Cover,
            Title = track.Title,
            Artists = [.. track.Artists],
            Status = track.Status,
            Progression = track.Progression,
            Duration = track.Duration,
            AlbumUrl = track.AlbumUrl,
        };
    }
}
