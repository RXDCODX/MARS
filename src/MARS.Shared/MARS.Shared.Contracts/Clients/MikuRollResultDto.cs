namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Результат мику-ролла для оверлея.
/// </summary>
public sealed record MikuRollResultDto(string MikuJson, int CollectedCount, int TotalCount);
