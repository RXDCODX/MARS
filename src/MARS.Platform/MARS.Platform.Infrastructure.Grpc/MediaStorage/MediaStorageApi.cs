using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using Grpc.Net.ClientFactory;
using MediaModel = MARS.Shared.Contracts.Models.Media;
using ProtoMedia = MARS.Shared.Contracts.MediaStorage;

namespace MARS.Platform.Infrastructure.Grpc.MediaStorage;

[ExcludeFromCodeCoverage]
internal sealed class MediaStorageApi(GrpcClientFactory clientFactory) : IMediaStorageApi
{
    public async Task<MediaModel.MediaInfo?> GetMediaInfoAsync(
        Guid mediaInfoId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .GetMediaInfoAsync(
                new ProtoMedia.GetMediaInfoRequest { Id = mediaInfoId.ToString() },
                cancellationToken: cancellationToken
            );

        return ToModel(response);
    }

    public async Task<MediaModel.MediaInfo?> GetNextRandomMemeAsync(
        string folderPath,
        string? displayName,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .GetNextRandomMemeAsync(
                new ProtoMedia.GetNextRandomMemeRequest
                {
                    FolderPath = folderPath,
                    DisplayName = displayName ?? string.Empty,
                },
                cancellationToken: cancellationToken
            );

        return ToModel(response);
    }

    public async Task<IReadOnlyList<MediaModel.MediaInfo>?> GetAllAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .GetAllAlertsAsync(
                new Google.Protobuf.WellKnownTypes.Empty(),
                cancellationToken: cancellationToken
            );

        return response.Alerts.Select(ToModel).ToList();
    }

    public async Task<string?> UploadMemoryFileAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .UploadMemoryFileAsync(
                new ProtoMedia.UploadMemoryFileRequest
                {
                    FileName = fileName,
                    Content = ByteString.CopyFrom(content),
                },
                cancellationToken: cancellationToken
            );

        return string.IsNullOrWhiteSpace(response.Path) ? null : response.Path;
    }

    public async Task<ProtoMedia.DownloadJobResponse> CreateDownloadJobAsync(
        ProtoMedia.CreateDownloadJobRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .CreateDownloadJobAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<ProtoMedia.DownloadJobResponse> GetDownloadJobAsync(
        ProtoMedia.GetDownloadJobRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .GetDownloadJobAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<ProtoMedia.DownloadJobResponse> CancelDownloadJobAsync(
        ProtoMedia.GetDownloadJobRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return await clientFactory
            .CreateClient<global::MARS.Shared.Contracts.MediaStorage.MediaStorageService.MediaStorageServiceClient>(
                "MediaStorage"
            )
            .CancelDownloadJobAsync(request, cancellationToken: cancellationToken);
    }

    private static MediaModel.MediaInfo ToModel(ProtoMedia.MediaInfo proto) =>
        new()
        {
            Id = Guid.TryParse(proto.Id, out var id) ? id : Guid.Empty,
            TextInfo = new MediaModel.MediaTextInfo
            {
                KeyWordsColor = proto.TextInfo.KeyWordsColor,
                TriggerWord = proto.TextInfo.TriggerWord,
                Text = proto.TextInfo.Text,
                TextColor = proto.TextInfo.TextColor,
                KeyWordSybmolDelimiter = proto.TextInfo.HasKeyWordSymbolDelimiter
                    ? (char?)proto.TextInfo.KeyWordSymbolDelimiter
                    : null,
            },
            FileInfo = new MediaModel.MediaFileInfo
            {
                Type = (MediaModel.MediaType)proto.FileInfo.Type,
                FilePath = proto.FileInfo.FilePath,
                IsLocalFile = proto.FileInfo.IsLocalFile,
                FileName = proto.FileInfo.FileName,
                Extension = proto.FileInfo.Extension,
                IsFileNotConvertable = proto.FileInfo.IsFileNotConvertable,
            },
            PositionInfo = new MediaModel.MediaPositionInfo
            {
                IsProportion = proto.PositionInfo.IsProportion,
                IsResizeRequires = proto.PositionInfo.IsResizeRequires,
                Height = proto.PositionInfo.Height,
                Width = proto.PositionInfo.Width,
                IsRotated = proto.PositionInfo.IsRotated,
                Rotation = proto.PositionInfo.Rotation,
                XCoordinate = proto.PositionInfo.XCoordinate,
                YCoordinate = proto.PositionInfo.YCoordinate,
                RandomCoordinates = proto.PositionInfo.RandomCoordinates,
                IsVerticallCenter = proto.PositionInfo.IsVerticallCenter,
                IsHorizontalCenter = proto.PositionInfo.IsHorizontalCenter,
                IsUseOriginalWidthAndHeight = proto.PositionInfo.IsUseOriginalWidthAndHeight,
            },
            MetaInfo = new MediaModel.MediaMetaInfo
            {
                TwitchPointsCost = proto.MetaInfo.TwitchPointsCost,
                TwitchGuid = string.IsNullOrWhiteSpace(proto.MetaInfo.TwitchGuid)
                    ? null
                    : Guid.Parse(proto.MetaInfo.TwitchGuid),
                Vip = proto.MetaInfo.Vip,
                DisplayName = proto.MetaInfo.DisplayName,
                IsLooped = proto.MetaInfo.IsLooped,
                IsFreezeRequired = proto.MetaInfo.IsFreezeRequired,
                Duration = proto.MetaInfo.Duration,
                Priority = (MediaModel.MediaAlertPriority)proto.MetaInfo.Priority,
                Volume = proto.MetaInfo.Volume,
                IsEnabled = proto.MetaInfo.IsEnabled,
            },
            StylesInfo = new MediaModel.MediaStylesInfo
            {
                IsBorder = proto.StylesInfo.IsBorder,
                IsShowLetterbox = proto.StylesInfo.IsShowLetterbox,
            },
        };
}
