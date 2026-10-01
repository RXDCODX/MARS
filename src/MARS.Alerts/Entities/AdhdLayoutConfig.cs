namespace MARS.Alerts.Entities;

/// <summary>
/// Настройка раскладки ADHD-экрана: единственная строка, задающая состав
/// декоративных виджетов и число логотипов.
/// </summary>
/// <remarks>
/// Перенесена из монолита вместе с настройкой, которую вносили вручную, но без
/// кода, который её читал: разметка оверлея осталась в mars.client, а методы
/// ReceiveConfig/ConfigUpdated в контракте TelegramusService отсутствуют. Поэтому
/// таблица пока только хранит состояние и ничего не отображает.
/// TODO(ADHD): либо вернуть доставку конфигурации в оверлей (методы в .proto +
/// фронтенд ADHDLayout), либо удалить таблицу вместе с отключённым оверлеем.
/// </remarks>
public class AdhdLayoutConfig
{
    public int Id { get; set; }

    public bool ShowRainEffect { get; set; }

    /// <summary>Каталог DVD-логотипов.</summary>
    public bool ShowDVDLogos { get; set; }

    public bool ShowBreakingNews { get; set; }
    public bool ShowStreamerVideo { get; set; }
    public bool ShowFitnessVideo { get; set; }
    public bool ShowGTAVideo { get; set; }
    public bool ShowHydraulicMobileVideo { get; set; }
    public bool ShowSlimeVideo { get; set; }
    public bool ShowMukbangVideo { get; set; }
    public bool ShowQuiz { get; set; }
    public bool ShowSurfer { get; set; }
    public bool ShowLOFIGirl { get; set; }
    public bool ShowCatisa { get; set; }
    public bool ShowNotifications { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Момент последнего изменения. В монолите nullable: строка создавалась
    /// одним INSERT без последующего UPDATE, поэтому у первоначальной записи
    /// значения не было.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public int DvdLogosCount { get; set; }
    public bool ShowTimer { get; set; }
}
