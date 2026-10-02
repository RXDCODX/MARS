namespace MARS.Shared.Clients;

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

/// <summary>
/// Ответ «список победителей»: обёртка нужна, чтобы отличать «пустой топ» от
/// «сервис не ответил» — клиент возвращает null при недоступности.
/// </summary>
public sealed record LeaderboardTop(IReadOnlyList<LeaderboardEntry> Entries);

/// <summary>
/// Место пользователя в таблице вместе с его строкой.
/// </summary>
public sealed record LeaderboardStats(int Place, LeaderboardEntry? User);
