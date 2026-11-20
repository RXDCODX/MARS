namespace MARS.Shared.Contracts.Media;

public enum MediaTypeKind
{
    None = 0,
    Image = 1,
    Audio = 2,
    Video = 3,
    TelegramSticker = 4,
    Voice = 5,
    Gif = 6,
}

public enum MediaAlertPriorityKind
{
    Low = 0,
    Normal = 1,
    High = 2,
}

public class MediaTextPayload
{
    public string KeywordsColor { get; set; } = "";
    public string TriggerWord { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextColor { get; set; } = "";
    public int KeywordSymbolDelimiter { get; set; }
    public bool HasKeywordSymbolDelimiter { get; set; }
}

public class MediaFilePayload
{
    public MediaTypeKind Type { get; set; }
    public string FilePath { get; set; } = "";
    public bool IsLocalFile { get; set; }
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public bool IsFileNotConvertable { get; set; }
}

public class MediaPositionPayload
{
    public bool IsProportion { get; set; }
    public bool IsResizeRequires { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public bool IsRotated { get; set; }
    public int Rotation { get; set; }
    public int XCoordinate { get; set; }
    public int YCoordinate { get; set; }
    public bool RandomCoordinates { get; set; }
    public bool IsVerticallCenter { get; set; }
    public bool IsHorizontalCenter { get; set; }
    public bool IsUseOriginalWidthAndHeight { get; set; }
}

public class MediaMetaPayload
{
    public int TwitchPointsCost { get; set; }
    public string TwitchGuid { get; set; } = "";
    public bool Vip { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsLooped { get; set; }
    public bool IsFreezeRequired { get; set; }
    public int Duration { get; set; }
    public MediaAlertPriorityKind Priority { get; set; }
    public int Volume { get; set; }
    public bool IsEnabled { get; set; }
}

public class MediaStylesPayload
{
    public bool IsBorder { get; set; }
    public bool IsShowLetterbox { get; set; }
}

public class MediaInfoPayload
{
    public string Id { get; set; } = "";
    public MediaTextPayload? TextInfo { get; set; }
    public MediaFilePayload? FileInfo { get; set; }
    public MediaPositionPayload? PositionInfo { get; set; }
    public MediaMetaPayload? MetaInfo { get; set; }
    public MediaStylesPayload? StylesInfo { get; set; }
}

public class MediaPayload
{
    public MediaInfoPayload? MediaInfo { get; set; }
    public string UploadStartTime { get; set; } = "";
}
