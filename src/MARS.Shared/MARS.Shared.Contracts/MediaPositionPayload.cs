namespace MARS.Shared.Contracts.Media;

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
