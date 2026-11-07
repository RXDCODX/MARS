namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Ответ «список победителей»: обёртка нужна, чтобы отличать «пустой топ» от
/// «сервис не ответил» — клиент возвращает null при недоступности.
/// </summary>
public sealed record LeaderboardTop(IReadOnlyList<LeaderboardEntry> Entries);
