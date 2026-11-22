using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models;

/// <summary>
/// Wolverine-событие: Telegram прислал случайный мем для оверлея.
/// Потребитель — MARS.Alerts (RandomMem-ветка хаба).
/// </summary>
public sealed class RandomMemRequested : IMarsEventMessage
{
    public required MediaInfo Media { get; init; }
}
