namespace MARS.Shikimori.Infrastructure.Clients;

public class ShikimoriClientOptions
{
    public const string SectionName = "Shikimori";

    public string ClientName { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
