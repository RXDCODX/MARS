namespace MARS.Alerts.Models;

public class BooruConfiguration
{
    public const string Section = "Booru";
    public string Login { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
