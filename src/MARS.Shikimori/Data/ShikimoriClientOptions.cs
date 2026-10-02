namespace MARS.Shikimori.Data;

public class ShikimoriClientOptions
{
    public const string SectionName = "Shikimori";

    public string ClientName { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string ShikimoriSite { get; set; } = "https://shikimori.one";
}
