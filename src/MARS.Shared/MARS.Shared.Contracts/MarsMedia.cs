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
