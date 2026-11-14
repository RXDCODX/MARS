namespace MARS.Shikimori.Application.Models;

/// <summary>Аниме в прикладной модели — без узлов GraphQL.</summary>
public record AnimeRef(long Id, string? Name, string? Russian, int? Year);
