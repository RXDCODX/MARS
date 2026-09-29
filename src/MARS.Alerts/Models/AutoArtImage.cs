namespace MARS.Alerts.Models;

public class AutoArtImage
{
    public string? Signature { get; set; }
    public string? Extension { get; set; }
    public int ImageID { get; set; }
    public int Favorites { get; set; }
    public string? DominantColor { get; set; }
    public string? Source { get; set; }
    public object? Artist { get; set; }
    public DateTime UploadedAt { get; set; }
    public object? LikedAt { get; set; }
    public bool IsNsfw { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ByteSize { get; set; }
    public string? URL { get; set; }
    public string? PreviewURL { get; set; }
}
