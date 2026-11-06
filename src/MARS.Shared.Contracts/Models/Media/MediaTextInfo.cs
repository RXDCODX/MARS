namespace MARS.Shared.Contracts.Models.Media;

public class MediaTextInfo
{
    public string? KeyWordsColor { get; set; }
    public string? TriggerWord { get; set; }
    public string? Text { get; set; }
    public string? TextColor { get; set; }
    public char? KeyWordSybmolDelimiter { get; set; } = '#';
}
