namespace MARS.Shared.Clients;

/// <summary>
/// Персонаж Shikimori в виде, в каком его возвращает новый сервис
/// <c>MARS.Shikimori</c>.
/// </summary>
/// <remarks>
/// Это доменная модель, а не транспорт GraphQL: монолит отдавал наружу узлы
/// <c>FullCharacter</c> самописного клиента, и потребители зависели от формы
/// чужого запроса. Здесь наружу уходит ровно то, что реально нужно — плюс
/// абсолютные URL, чтобы потребителям не приходилось знать адрес сайта.
/// <para>
/// <c>ImagePath</c> — путь так, как его отдал Shikimori. В базе
/// MARS.WaifuGacha картинки хранятся именно в этой форме (так их нормализует
/// <c>WaifuRollController</c> на ссылках от UI), а <c>ImageUrl</c> — готовый
/// адрес для тех, кому склеивать ничего не нужно.
/// </para>
/// <para>
/// <c>AnimeTitle</c> и <c>MangaTitle</c> — самые короткие названия среди
/// произведений персонажа: правило из монолитного
/// <c>ShikimoriService.GetCharacterAnimeTitle</c>, перенесено в
/// <c>MARS.Shikimori</c>, где и живут данные.
/// </para>
/// </remarks>
public sealed record ShikimoriCharacterRef(
    long Id,
    string Name,
    string? RussianName,
    string? Description,
    string ImageUrl,
    string ImagePath,
    string? AnimeTitle,
    string? MangaTitle
);

/// <summary>
/// Состояние рейт-лимитера Shikimori: сколько запросов доступно и когда
/// окно сдвинется.
/// </summary>
/// <remarks>
/// Время в секундах, а не <c>TimeSpan</c>: так его не испортит сериализация
/// <c>System.Text.Json</c> в разных версиях формата.
/// </remarks>
public sealed record ShikimoriRateLimiterInfo(
    int AvailablePerSecond,
    int AvailablePerMinute,
    double SecondsToResetSecondWindow,
    double SecondsToResetMinuteWindow
);
