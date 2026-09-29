namespace MARS.TwitchCore.Configuration;

public class AudioControllerOptions
{
    public const string SectionName = "HttpClientsConfiguration";

    public int AudioControllerDevPort { get; set; } = 30691;
    public int AudioControllerProdPort { get; set; } = 30695;
}
