using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.MediaStorage.Application.Alerts;
using MARS.MediaStorage.Application.Downloads;
using MARS.MediaStorage.Application.MemoryStorage;
using MARS.MediaStorage.Application.RandomMemes;
using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.MediaStorage;
using Microsoft.Extensions.Logging;
using MediaModel = MARS.Shared.Contracts.Models.Media;
using ProtoMedia = MARS.Shared.Contracts.MediaStorage;

namespace MARS.MediaStorage.Api.Rest.Grpc;

public sealed class MediaStorageGrpcService(
    IAlertService alerts,
    IRandomMemePlaybackService playbackService,
    IMediaDownloadJobService downloadJobs
) : MediaStorageService.MediaStorageServiceBase
{
    public override async Task<ProtoMedia.MediaInfo> GetMediaInfo(
        ProtoMedia.GetMediaInfoRequest request,
        ServerCallContext context
    )
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Некорректный идентификатор медиа")
            );
        }

        var alert = await alerts.GetAsync(id, context.CancellationToken);
        if (alert is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Алерт с ID '{id}' не найден"));
        }

        return ToProto(alert);
    }

    public override async Task<ProtoMedia.MediaInfo> GetNextRandomMeme(
        ProtoMedia.GetNextRandomMemeRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.FolderPath))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Папка очереди не указана")
            );
        }

        var media = await playbackService.GetNextPreparedMediaAsync(
            request.FolderPath,
            request.DisplayName,
            context.CancellationToken
        );
        if (media is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Медиа не найдено"));
        }

        return ToProto(media);
    }

    public override async Task<ProtoMedia.GetAllAlertsResponse> GetAllAlerts(
        Google.Protobuf.WellKnownTypes.Empty request,
        ServerCallContext context
    )
    {
        var stored = await alerts.GetAllAsync(context.CancellationToken);
        var response = new ProtoMedia.GetAllAlertsResponse();
        response.Alerts.AddRange(stored.Select(ToProto));
        return response;
    }

    public override async Task<ProtoMedia.UploadMemoryFileResponse> UploadMemoryFile(
        ProtoMedia.UploadMemoryFileRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Имя файла не может быть пустым")
            );
        }

        byte[] content;
        try
        {
            content = request.Content.ToByteArray();
        }
        catch (Exception ex)
        {
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    $"Содержимое файла не в формате base64: {ex.Message}"
                )
            );
        }

        if (content.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Файл пуст"));
        }

        const int maxBytes = 32 * 1024 * 1024;
        if (content.Length > maxBytes)
        {
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    $"Файл слишком большой: {content.Length} байт"
                )
            );
        }

        var path = await MemoryStorage.AddFileAsync(request.FileName, content);
        return new ProtoMedia.UploadMemoryFileResponse { Path = path };
    }

    public override async Task<ProtoMedia.DownloadJobResponse> CreateDownloadJob(
        ProtoMedia.CreateDownloadJobRequest request,
        ServerCallContext context
    )
    {
        var job = await downloadJobs.CreateAsync(request, context.CancellationToken);
        return ToDownloadJobResponse(job);
    }

    public override async Task<ProtoMedia.DownloadJobResponse> GetDownloadJob(
        ProtoMedia.GetDownloadJobRequest request,
        ServerCallContext context
    )
    {
        MediaDownloadJob? job;
        if (Guid.TryParse(request.JobId, out var jobId))
        {
            job = await downloadJobs.GetAsync(jobId, context.CancellationToken);
        }
        else
        {
            job = await downloadJobs.GetByShortIdAsync(request.JobId, context.CancellationToken);
        }

        if (job is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Задача не найдена"));
        }

        return ToDownloadJobResponse(job);
    }

    public override async Task<ProtoMedia.DownloadJobResponse> CancelDownloadJob(
        ProtoMedia.GetDownloadJobRequest request,
        ServerCallContext context
    )
    {
        MediaDownloadJob? job;
        if (Guid.TryParse(request.JobId, out var jobId))
        {
            job = await downloadJobs.CancelAsync(jobId, context.CancellationToken);
        }
        else
        {
            var found = await downloadJobs.GetByShortIdAsync(request.JobId, context.CancellationToken);
            job = found is null ? null : await downloadJobs.CancelAsync(found.Id, context.CancellationToken);
        }
        if (job is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Задача не найдена"));
        }

        return ToDownloadJobResponse(job);
    }

    private static ProtoMedia.DownloadJobResponse ToDownloadJobResponse(MediaDownloadJob job) =>
        new()
        {
            JobId = job.Id.ToString(),
            ShortJobId = job.Id.ToString("N")[..8],
            Status = (int)job.Status,
            SourceKind = job.SourceKind,
            Title = job.Title,
            FailureCategory = job.FailureCategory ?? string.Empty,
            FailureReason = job.FailureReason ?? string.Empty,
            ArtifactUrl = job.ArtifactUrl ?? string.Empty,
            ArtifactFileName = job.ArtifactFileName ?? string.Empty,
            ArtifactMimeType = job.ArtifactMimeType ?? string.Empty,
            ArtifactKind = (int)job.ArtifactKind,
            ArtifactSizeBytes = job.ArtifactSizeBytes,
            ArtifactDurationSeconds = job.ArtifactDurationSeconds,
            ArtifactWidth = job.ArtifactWidth,
            ArtifactHeight = job.ArtifactHeight,
            Trimmed = job.Trimmed,
            Caption = job.Caption ?? string.Empty,
            AttemptCount = job.AttemptCount,
            ProcessingTimeMs = job.ProcessingTimeMs,
            Stage = job.Stage ?? string.Empty,
            Profile = job.Profile ?? string.Empty,
            CreatedAtUtc = job.CreatedAtUtc.ToString("O"),
            CompletedAtUtc = job.CompletedAtUtc?.ToString("O") ?? string.Empty,
        };

    private static ProtoMedia.MediaInfo ToProto(MediaModel.MediaInfo source) =>
        new()
        {
            Id = source.Id.ToString(),
            TextInfo = new ProtoMedia.MediaTextInfo
            {
                KeyWordsColor = source.TextInfo.KeyWordsColor ?? string.Empty,
                TriggerWord = source.TextInfo.TriggerWord ?? string.Empty,
                Text = source.TextInfo.Text ?? string.Empty,
                TextColor = source.TextInfo.TextColor ?? string.Empty,
                KeyWordSymbolDelimiter = source.TextInfo.KeyWordSybmolDelimiter.HasValue
                    ? source.TextInfo.KeyWordSybmolDelimiter.Value
                    : 0,
                HasKeyWordSymbolDelimiter = source.TextInfo.KeyWordSybmolDelimiter.HasValue,
            },
            FileInfo = new ProtoMedia.MediaFileInfo
            {
                Type = (int)source.FileInfo.Type,
                FilePath = source.FileInfo.FilePath,
                IsLocalFile = source.FileInfo.IsLocalFile,
                FileName = source.FileInfo.FileName,
                Extension = source.FileInfo.Extension,
                IsFileNotConvertable = source.FileInfo.IsFileNotConvertable,
            },
            PositionInfo = new ProtoMedia.MediaPositionInfo
            {
                IsProportion = source.PositionInfo.IsProportion,
                IsResizeRequires = source.PositionInfo.IsResizeRequires,
                Height = source.PositionInfo.Height,
                Width = source.PositionInfo.Width,
                IsRotated = source.PositionInfo.IsRotated,
                Rotation = source.PositionInfo.Rotation,
                XCoordinate = source.PositionInfo.XCoordinate,
                YCoordinate = source.PositionInfo.YCoordinate,
                RandomCoordinates = source.PositionInfo.RandomCoordinates,
                IsVerticallCenter = source.PositionInfo.IsVerticallCenter,
                IsHorizontalCenter = source.PositionInfo.IsHorizontalCenter,
                IsUseOriginalWidthAndHeight = source.PositionInfo.IsUseOriginalWidthAndHeight,
            },
            MetaInfo = new ProtoMedia.MediaMetaInfo
            {
                TwitchPointsCost = source.MetaInfo.TwitchPointsCost,
                TwitchGuid = source.MetaInfo.TwitchGuid?.ToString() ?? string.Empty,
                Vip = source.MetaInfo.Vip,
                DisplayName = source.MetaInfo.DisplayName,
                IsLooped = source.MetaInfo.IsLooped,
                IsFreezeRequired = source.MetaInfo.IsFreezeRequired,
                Duration = source.MetaInfo.Duration,
                Priority = (int)source.MetaInfo.Priority,
                Volume = source.MetaInfo.Volume,
                IsEnabled = source.MetaInfo.IsEnabled,
            },
            StylesInfo = new ProtoMedia.MediaStylesInfo
            {
                IsBorder = source.StylesInfo.IsBorder,
                IsShowLetterbox = source.StylesInfo.IsShowLetterbox,
            },
        };
}
