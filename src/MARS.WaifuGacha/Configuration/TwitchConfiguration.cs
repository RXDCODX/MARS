namespace MARS.WaifuGacha.Configuration;

public class TwitchConfiguration
{
    public static string SectionName { get; set; } = "TwitchConfig";

    public string OAuth { get; set; } = string.Empty;

    public string Channel { get; set; } = "rxdcodx";
}
