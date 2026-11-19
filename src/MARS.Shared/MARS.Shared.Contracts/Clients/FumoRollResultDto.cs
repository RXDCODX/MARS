namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Результат фумо-ролла для оверлея.
/// </summary>
public sealed record FumoRollResultDto(string FumoJson, int CollectedCount, int TotalCount);
