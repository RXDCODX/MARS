namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Строка таблицы лидеров мини-игр в том виде, в каком её отдаёт
/// <c>MARS.TwitchCore</c> и читает <c>MARS.Commands</c>.
/// </summary>
/// <remarks>
/// Тип лежит в MARS.Shared, а не в MARS.TwitchCore: его одновременно сериализует
/// контроллер-владелец и десериализует клиент команды, и без общего типа они
/// разошлись бы по именам свойств молча.
/// </remarks>
public sealed record LeaderboardEntry(
    string TwitchId,
    string? DisplayName,
    int TotalWins,
    int RussianRouletteWins,
    int TriviaWins
);
