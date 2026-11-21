namespace MARS.Shared.Contracts.Media;

public class MediaFilePayload
{
    public MediaTypeKind Type { get; set; }
    public string FilePath { get; set; } = "";
    public bool IsLocalFile { get; set; }
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public bool IsFileNotConvertable { get; set; }
}
