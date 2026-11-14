namespace MARS.Shikimori.Application.Configuration;

/// <summary>Настройки приложения, используемые при формировании ссылок Shikimori.</summary>
public sealed class ShikimoriSettings
{
    public const string SectionName = "Shikimori";

    public string ShikimoriSite { get; set; } = "https://shikimori.one";
}
