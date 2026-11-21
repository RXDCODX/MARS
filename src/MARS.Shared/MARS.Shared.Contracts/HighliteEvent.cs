using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class HighliteEvent
{
    public string MessageJson { get; set; } = "";
    public string Color { get; set; } = "";
    public string FaceUrlJson { get; set; } = "";
}
