namespace MARS.Alerts.Models;

/// <summary>
/// Настройка раскладки ADHD-экрана в том виде, в каком её получает оверлей.
/// </summary>
/// <remarks>
/// Значения по умолчанию повторяют монолитные: пустая таблица означает «показать
/// весь набор виджетов», а не «всё выключено». Сущность
/// <c>MARS.Alerts.Entities.AdhdLayoutConfig</c> дефолтов не имеет — она
/// описывает сохранённую строку, а не то, что увидит оверлей.
/// </remarks>
public class AdhdLayoutConfigDto
{
    public bool ShowRainEffect { get; set; } = true;
    public bool ShowDVDLogos { get; set; } = true;
    public bool ShowBreakingNews { get; set; } = true;
    public bool ShowStreamerVideo { get; set; } = true;
    public bool ShowFitnessVideo { get; set; } = true;
    public bool ShowGTAVideo { get; set; } = true;
    public bool ShowHydraulicMobileVideo { get; set; } = true;
    public bool ShowSlimeVideo { get; set; } = true;
    public bool ShowMukbangVideo { get; set; } = true;
    public bool ShowQuiz { get; set; } = true;
    public bool ShowSurfer { get; set; } = true;
    public bool ShowLOFIGirl { get; set; } = true;
    public bool ShowCatisa { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool ShowTimer { get; set; } = true;
    public int DvdLogosCount { get; set; } = 12;
}
