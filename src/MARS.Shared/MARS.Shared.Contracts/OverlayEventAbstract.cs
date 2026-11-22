namespace MARS.Shared.Contracts;

/// <summary>
/// Маркер события оверлейного хаба. Наследники — допустимые ветви
/// <see cref="TelegramusEvent"/>; контроллер входа находит активную ветвь
/// и сверяет тип payload с параметром метода IAlertsHub по этому маркеру.
/// </summary>
public abstract class OverlayEventAbstract { }
