namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>
/// Контракт глобального поиска для UI. Пути остаются относительными к wwwroot.
/// </summary>
public sealed record MediaStorageSearchResultDto(
    IReadOnlyList<MediaStorageEntryDto> Entries,
    int Total,
    int Page,
    int PageSize
);
