namespace MARS.SoundRequest.Configuration;

public class HttpClientsConfiguration
{
    public const string Configuration = "HttpClientsConfiguration";

    public int AudioControllerDevPort { get; set; } = 30691;
    public int AudioControllerProdPort { get; set; } = 30695;
}
