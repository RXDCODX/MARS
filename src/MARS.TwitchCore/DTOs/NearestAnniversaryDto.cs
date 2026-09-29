namespace MARS.TwitchCore.DTOs;

public class NearestAnniversaryDto
{
    public required string TwitchId { get; set; }
    public required string DisplayName { get; set; }
    public required string AnniversaryName { get; set; }
    public DateTime AnniversaryDate { get; set; }
    public int Months { get; set; }
}
