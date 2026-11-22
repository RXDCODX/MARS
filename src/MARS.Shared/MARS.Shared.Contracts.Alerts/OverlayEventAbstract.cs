namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

/// <summary>
/// Маркер события оверлейного хаба. Наследники — допустимые ветви конверта;
/// контроллер входа находит активную ветвь и сверяет тип payload с параметром
/// метода IAlertsHub по этому маркеру.
/// </summary>
public abstract class OverlayEventAbstract;
