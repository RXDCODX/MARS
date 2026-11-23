namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

public class PostTwitchInfoEvent : OverlayEventAbstract
{
    /// <summary>
    /// Публичное отображаемое имя стримера. Секреты (clientId/secret) через
    /// этот канал не передаются: они не должны уходить в браузер.
    /// </summary>
    public string DisplayName { get; set; } = "";

    public string? ProfileImageUrl { get; set; }

    public string? ChatColor { get; set; }
}
