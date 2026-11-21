namespace MARS.Shared.Contracts.Media;

public class MediaInfoPayload
{
    public string Id { get; set; } = "";
    public MediaTextPayload? TextInfo { get; set; }
    public MediaFilePayload? FileInfo { get; set; }
    public MediaPositionPayload? PositionInfo { get; set; }
    public MediaMetaPayload? MetaInfo { get; set; }
    public MediaStylesPayload? StylesInfo { get; set; }
}
