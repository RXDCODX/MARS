using System.Text.Json;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Kernel;
using AdhdContractConfig = MARS.Shared.Contracts.Models.AdhdLayoutConfigDto;
using AdhdModuleConfig = MARS.Modules.Alerts.Application.Adhd.Models.AdhdLayoutConfigDto;

namespace MARS.Alerts.Presentation.Overlay.Hubs;

/// <summary>
/// Раскладка ADHD-экрана в том виде, в каком она едет клиенту.
/// </summary>
/// <remarks>
/// <para>
/// Полезная нагрузка — <c>bytes</c>: так же, как остальные объектные поля
/// контракта оверлея, настройка едет JSON'ом внутри события. Клиент разбирает
/// её тем же кодом, что и раньше, поэтому форма события не меняется.
/// </para>
/// <para>
/// Имена свойств — camelCase: ровно их ждёт разбор на клиенте и ровно их
/// отдавал броадкастер, пока настройка шла через gRPC.
/// </para>
/// </remarks>
internal static class AdhdConfigPayload
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Собирает событие с раскладкой.
    /// </summary>
    public static AdhdConfigEvent ToEvent(AdhdModuleConfig config) =>
        new()
        {
            Config = new AdhdContractConfig
            {
                ShowRainEffect = config.ShowRainEffect,
                ShowDVDLogos = config.ShowDVDLogos,
                ShowBreakingNews = config.ShowBreakingNews,
                ShowStreamerVideo = config.ShowStreamerVideo,
                ShowFitnessVideo = config.ShowFitnessVideo,
                ShowGTAVideo = config.ShowGTAVideo,
                ShowHydraulicMobileVideo = config.ShowHydraulicMobileVideo,
                ShowSlimeVideo = config.ShowSlimeVideo,
                ShowMukbangVideo = config.ShowMukbangVideo,
                ShowQuiz = config.ShowQuiz,
                ShowSurfer = config.ShowSurfer,
                ShowLOFIGirl = config.ShowLOFIGirl,
                ShowCatisa = config.ShowCatisa,
                ShowNotifications = config.ShowNotifications,
                ShowTimer = config.ShowTimer,
                DvdLogosCount = config.DvdLogosCount,
            },
        };
}
