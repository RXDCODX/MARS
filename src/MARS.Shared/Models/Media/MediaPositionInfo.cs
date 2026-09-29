namespace MARS.Shared.Models.Media;

public class MediaPositionInfo
{
    public bool IsProportion { get; set; } = true;
    public bool IsResizeRequires { get; set; } = false;
    public int Height { get; set; } = 500;
    public int Width { get; set; } = 500;
    public bool IsRotated { get; set; } = true;
    public int Rotation { get; set; } = Random.Shared.Next(0, 41);
    public int XCoordinate { get; set; } = 0;
    public int YCoordinate { get; set; } = 0;
    public bool RandomCoordinates { get; set; } = true;
    public bool IsVerticallCenter { get; set; } = false;
    public bool IsHorizontalCenter { get; set; } = false;
    public bool IsUseOriginalWidthAndHeight { get; set; } = true;
}
