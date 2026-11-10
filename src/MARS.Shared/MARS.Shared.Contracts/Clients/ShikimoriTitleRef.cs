namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Ссылка на произведение на Shikimori в том виде, в каком его показывает
/// команда: id, названия, год и готовая ссылка.
/// </summary>
public sealed record ShikimoriTitleRef(
    long Id,
    string? Name,
    string? RussianName,
    int? Year,
    string Url
);
