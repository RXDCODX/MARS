namespace MARS.Shikimori.Application.Models;

/// <summary>Манга в доменном виде.</summary>
public record MangaRef(long Id, string? Name, string? Russian, int? Year);
