using MARS.Shared.Grpc.Scoreboard;

namespace MARS.Scoreboard.Hubs.Interfaces;

/// <summary>
/// Методы хаба табло, которые сервер вызывает у клиента.
/// </summary>
/// <remarks>
/// Только направление сервер → клиент. Вызовы панели администратора
/// (<c>UpdateState</c>, <c>SetVisibility</c>, <c>UpdatePlayerScore</c>,
/// <c>SetPlayerFinal</c>) методами самого <see cref="Hubs.ScoreboardHub"/>: у
/// <c>Hub&lt;T&gt;</c> параметр обобщения — это контракт клиента, и класть туда
/// серверные вызовы нельзя.
/// <para>
/// Хаба на сервере не было, хотя <c>MARS.Scoreboard</c> держал
/// <c>GrpcEventBroadcaster&lt;ScoreboardEvent&gt;</c>. Клиент подписывался на
/// <c>hubs/scoreboard</c> и получал 405: путь не был объявлен в Gateway.
/// </para>
/// </remarks>
public interface IScoreboardHub
{
    /// <summary>
    /// Полное состояние табло: игроки, очки, цвета, раскладка, видимость.
    /// </summary>
    /// <remarks>
    /// Имя взято из клиентского кода, где метод хаба назывался
    /// <c>ReceiveState</c>. Оставлено прежним: переименование потребовало бы
    /// правки клиента без выигрыша.
    /// </remarks>
    Task ReceiveState(ScoreboardSnapshot stateUpdated);
}
