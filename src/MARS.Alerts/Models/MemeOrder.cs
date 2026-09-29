namespace MARS.Alerts.Models;

public class MemeOrder
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public int Order { get; set; }
    public required string FilePath { get; set; }
    public int? MemeTypeId { get; set; }
    public MemeType? Type { get; set; }
    public bool IsFileNotConvertable { get; set; }
}
