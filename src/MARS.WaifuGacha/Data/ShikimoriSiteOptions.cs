namespace MARS.WaifuGacha.Data;

/// <summary>
/// Адрес сайта Shikimori для MARS.WaifuGacha.
/// </summary>
/// <remarks>
/// Реквизиты приложения Shikimori живут только в <c>MARS.Shikimori</c> — там
/// клиент и рейт-лимитер. Здесь остаётся ровно одно: <c>WaifuRollController</c>
/// нормализует входящие от UI ссылки на картинки, и для этого нужно знать адрес
/// сайта. Свою секцию не заводим: обе настройки читаются из одной секции
/// <c>Shikimori</c>.
/// </remarks>
public class ShikimoriSiteOptions
{
    public const string SectionName = "Shikimori";

    public string ShikimoriSite { get; set; } = "https://shikimori.one";
}
