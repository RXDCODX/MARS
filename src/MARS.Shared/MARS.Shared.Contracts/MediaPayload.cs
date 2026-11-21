namespace MARS.Shared.Contracts.Media;

public class MediaPayload
{
    public MediaInfoPayload? MediaInfo { get; set; }
    public string UploadStartTime { get; set; } = "";
}
