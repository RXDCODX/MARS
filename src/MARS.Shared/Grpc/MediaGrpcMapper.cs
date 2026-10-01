using MARS.Shared.Grpc.Media;
using MARS.Shared.Models.Media;
using ProtoMediaType = MARS.Shared.Grpc.Media.MediaTypeKind;
using ProtoPriority = MARS.Shared.Grpc.Media.MediaAlertPriorityKind;

namespace MARS.Shared.Grpc;

public static class MediaGrpcMapper
{
    public static MediaPayload ToProto(MediaDto dto)
    {
        return new MediaPayload
        {
            MediaInfo = ToProto(dto.MediaInfo),
            UploadStartTime = dto.UploadStartTime.ToString("O"),
        };
    }

    public static MediaInfoPayload ToProto(MediaInfo info)
    {
        return new MediaInfoPayload
        {
            Id = info.Id.ToString(),
            TextInfo = ToProto(info.TextInfo),
            FileInfo = ToProto(info.FileInfo),
            PositionInfo = ToProto(info.PositionInfo),
            MetaInfo = ToProto(info.MetaInfo),
            StylesInfo = ToProto(info.StylesInfo),
        };
    }

    public static MediaDto ToDto(MediaPayload payload)
    {
        return new MediaDto(ToDto(payload.MediaInfo))
        {
            UploadStartTime = ParseDateTime(payload.UploadStartTime),
        };
    }

    public static MediaInfo ToDto(MediaInfoPayload payload)
    {
        return new MediaInfo
        {
            Id = ParseGuid(payload.Id),
            TextInfo = ToDto(payload.TextInfo),
            FileInfo = ToDto(payload.FileInfo),
            PositionInfo = ToDto(payload.PositionInfo),
            MetaInfo = ToDto(payload.MetaInfo),
            StylesInfo = ToDto(payload.StylesInfo),
        };
    }

    public static ProtoMediaType ToProto(MediaType type)
    {
        return type switch
        {
            MediaType.Image => ProtoMediaType.Image,
            MediaType.Audio => ProtoMediaType.Audio,
            MediaType.Video => ProtoMediaType.Video,
            MediaType.TelegramSticker => ProtoMediaType.TelegramSticker,
            MediaType.Voice => ProtoMediaType.Voice,
            MediaType.Gif => ProtoMediaType.Gif,
            _ => ProtoMediaType.None,
        };
    }

    public static MediaType ToMediaType(ProtoMediaType type)
    {
        return type switch
        {
            ProtoMediaType.Image => MediaType.Image,
            ProtoMediaType.Audio => MediaType.Audio,
            ProtoMediaType.Video => MediaType.Video,
            ProtoMediaType.TelegramSticker => MediaType.TelegramSticker,
            ProtoMediaType.Voice => MediaType.Voice,
            ProtoMediaType.Gif => MediaType.Gif,
            _ => MediaType.None,
        };
    }

    public static ProtoPriority ToProto(MediaAlertPriority priority)
    {
        return priority switch
        {
            MediaAlertPriority.Low => ProtoPriority.Low,
            MediaAlertPriority.High => ProtoPriority.High,
            _ => ProtoPriority.Normal,
        };
    }

    public static MediaAlertPriority ToMediaAlertPriority(ProtoPriority priority)
    {
        return priority switch
        {
            ProtoPriority.Low => MediaAlertPriority.Low,
            ProtoPriority.High => MediaAlertPriority.High,
            _ => MediaAlertPriority.Normal,
        };
    }

    public static DateTime ParseDateTime(string value)
    {
        return DateTime.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed
        )
            ? parsed
            : DateTime.MinValue;
    }

    public static Guid ParseGuid(string value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
    }

    private static MediaTextPayload ToProto(MediaTextInfo textInfo)
    {
        return new MediaTextPayload
        {
            KeywordsColor = textInfo.KeyWordsColor ?? string.Empty,
            TriggerWord = textInfo.TriggerWord ?? string.Empty,
            Text = textInfo.Text ?? string.Empty,
            TextColor = textInfo.TextColor ?? string.Empty,
            KeywordSymbolDelimiter = textInfo.KeyWordSybmolDelimiter ?? '\0',
            HasKeywordSymbolDelimiter = textInfo.KeyWordSybmolDelimiter.HasValue,
        };
    }

    private static MediaTextInfo ToDto(MediaTextPayload payload)
    {
        return new MediaTextInfo
        {
            KeyWordsColor = payload.KeywordsColor,
            TriggerWord = payload.TriggerWord,
            Text = payload.Text,
            TextColor = payload.TextColor,
            KeyWordSybmolDelimiter = payload.HasKeywordSymbolDelimiter
                ? (char)payload.KeywordSymbolDelimiter
                : null,
        };
    }

    private static MediaFilePayload ToProto(MediaFileInfo fileInfo)
    {
        return new MediaFilePayload
        {
            Type = ToProto(fileInfo.Type),
            FilePath = fileInfo.FilePath ?? string.Empty,
            IsLocalFile = fileInfo.IsLocalFile,
            FileName = fileInfo.FileName ?? string.Empty,
            Extension = fileInfo.Extension ?? string.Empty,
            IsFileNotConvertable = fileInfo.IsFileNotConvertable,
        };
    }

    private static MediaFileInfo ToDto(MediaFilePayload payload)
    {
        return new MediaFileInfo
        {
            Type = ToMediaType(payload.Type),
            FilePath = payload.FilePath,
            IsLocalFile = payload.IsLocalFile,
            FileName = payload.FileName,
            Extension = payload.Extension,
            IsFileNotConvertable = payload.IsFileNotConvertable,
        };
    }

    private static MediaPositionPayload ToProto(MediaPositionInfo positionInfo)
    {
        return new MediaPositionPayload
        {
            IsProportion = positionInfo.IsProportion,
            IsResizeRequires = positionInfo.IsResizeRequires,
            Height = positionInfo.Height,
            Width = positionInfo.Width,
            IsRotated = positionInfo.IsRotated,
            Rotation = positionInfo.Rotation,
            XCoordinate = positionInfo.XCoordinate,
            YCoordinate = positionInfo.YCoordinate,
            RandomCoordinates = positionInfo.RandomCoordinates,
            IsVerticallCenter = positionInfo.IsVerticallCenter,
            IsHorizontalCenter = positionInfo.IsHorizontalCenter,
            IsUseOriginalWidthAndHeight = positionInfo.IsUseOriginalWidthAndHeight,
        };
    }

    private static MediaPositionInfo ToDto(MediaPositionPayload payload)
    {
        return new MediaPositionInfo
        {
            IsProportion = payload.IsProportion,
            IsResizeRequires = payload.IsResizeRequires,
            Height = payload.Height,
            Width = payload.Width,
            IsRotated = payload.IsRotated,
            Rotation = payload.Rotation,
            XCoordinate = payload.XCoordinate,
            YCoordinate = payload.YCoordinate,
            RandomCoordinates = payload.RandomCoordinates,
            IsVerticallCenter = payload.IsVerticallCenter,
            IsHorizontalCenter = payload.IsHorizontalCenter,
            IsUseOriginalWidthAndHeight = payload.IsUseOriginalWidthAndHeight,
        };
    }

    private static MediaMetaPayload ToProto(MediaMetaInfo metaInfo)
    {
        return new MediaMetaPayload
        {
            TwitchPointsCost = metaInfo.TwitchPointsCost,
            TwitchGuid = metaInfo.TwitchGuid?.ToString() ?? string.Empty,
            Vip = metaInfo.Vip,
            DisplayName = metaInfo.DisplayName ?? string.Empty,
            IsLooped = metaInfo.IsLooped,
            IsFreezeRequired = metaInfo.IsFreezeRequired,
            Duration = metaInfo.Duration,
            Priority = ToProto(metaInfo.Priority),
            Volume = metaInfo.Volume,
            IsEnabled = metaInfo.IsEnabled,
        };
    }

    private static MediaMetaInfo ToDto(MediaMetaPayload payload)
    {
        return new MediaMetaInfo
        {
            TwitchPointsCost = payload.TwitchPointsCost,
            TwitchGuid = string.IsNullOrEmpty(payload.TwitchGuid)
                ? null
                : ParseGuid(payload.TwitchGuid),
            Vip = payload.Vip,
            DisplayName = payload.DisplayName,
            IsLooped = payload.IsLooped,
            IsFreezeRequired = payload.IsFreezeRequired,
            Duration = payload.Duration,
            Priority = ToMediaAlertPriority(payload.Priority),
            Volume = payload.Volume,
            IsEnabled = payload.IsEnabled,
        };
    }

    private static MediaStylesPayload ToProto(MediaStylesInfo stylesInfo)
    {
        return new MediaStylesPayload
        {
            IsBorder = stylesInfo.IsBorder,
            IsShowLetterbox = stylesInfo.IsShowLetterbox,
        };
    }

    private static MediaStylesInfo ToDto(MediaStylesPayload payload)
    {
        return new MediaStylesInfo
        {
            IsBorder = payload.IsBorder,
            IsShowLetterbox = payload.IsShowLetterbox,
        };
    }
}
