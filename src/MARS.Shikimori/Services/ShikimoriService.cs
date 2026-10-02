using MARS.Shared.Clients;
using MARS.Shikimori.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shikimori.Services;

/// <summary>
/// Единственный владелец клиента Shikimori. Наружу отдаёт доменные контракты
/// (<see cref="ShikimoriTitleRef"/>, <see cref="ShikimoriCharacterRef"/>) с
/// абсолютными URL, а не узлы GraphQL.
/// </summary>
/// <remarks>
/// Перенос <c>MARS.WaifuGacha/Services/ShikimoriService.cs</c> (пункты W1, W3).
/// Отличие от монолита в одном: адрес сайта известен только здесь, поэтому
/// наружу уходят готовые ссылки и картинки. Потребителям больше не нужно знать
/// про <c>ShikimoriSite</c> — раньше они склеивали его с относительным путём
/// сами, и это повторилось в четырёх местах.
///
/// Сам клиент спрятан за <see cref="IShikimoriClient"/>: без разрыва каждый
/// вызов уходил бы в сеть, и проверялся бы не код, а доступность Shikimori.
/// </remarks>
public class ShikimoriService
{
    /// <summary>Порог оценки для случайных произведений — из монолита.</summary>
    private const int MinimumScore = 7;

    private readonly ILogger<ShikimoriService> _logger;
    private readonly IShikimoriRateLimiter _rateLimiter;
    private readonly IShikimoriClient _client;
    private readonly ShikimoriClientOptions _options;

    public ShikimoriService(
        ILogger<ShikimoriService> logger,
        IOptions<ShikimoriClientOptions> configuration,
        IShikimoriRateLimiter rateLimiter,
        IShikimoriClient client
    )
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
        _client = client;
        _options = configuration.Value;
    }

    /// <summary>Случайное аниме с оценкой не ниже <see cref="MinimumScore"/>.</summary>
    public async Task<ShikimoriTitleRef?> GetRandomAnimeAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = default(ShikimoriTitleRef?);

        try
        {
            await _rateLimiter.WaitForSlotAsync(cancellationToken);

            var anime = await _client.GetRandomAnimeAsync(MinimumScore, cancellationToken);

            if (anime is not null)
            {
                result = new ShikimoriTitleRef(
                    anime.Id,
                    anime.Name,
                    anime.Russian,
                    anime.Year,
                    BuildUrl($"/animes/{anime.Id}")
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении случайного аниме");
        }

        return result;
    }

    /// <summary>Случайная манга с оценкой не ниже <see cref="MinimumScore"/>.</summary>
    public async Task<ShikimoriTitleRef?> GetRandomMangaAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = default(ShikimoriTitleRef?);

        try
        {
            await _rateLimiter.WaitForSlotAsync(cancellationToken);

            var manga = await _client.GetRandomMangaAsync(MinimumScore, cancellationToken);

            if (manga is not null)
            {
                result = new ShikimoriTitleRef(
                    manga.Id,
                    manga.Name,
                    manga.Russian,
                    manga.Year,
                    BuildUrl($"/mangas/{manga.Id}")
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении случайной манги");
        }

        return result;
    }

    /// <summary>
    /// Персонаж по id. Названия аниме и манги — самые короткие среди
    /// произведений персонажа, как выбирал монолитный
    /// <c>GetCharacterAnimeTitle</c>: длинные японские названия в чате читаются
    /// хуже.
    /// </summary>
    public async Task<ShikimoriCharacterRef?> GetCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        var result = default(ShikimoriCharacterRef?);

        if (id > 0)
        {
            try
            {
                await _rateLimiter.WaitForSlotAsync(cancellationToken);

                var character = await _client.GetCharacterAsync(id, cancellationToken);

                if (character is not null)
                {
                    result = new ShikimoriCharacterRef(
                        character.Id,
                        character.Name,
                        character.Russian,
                        character.Description,
                        BuildUrl(character.ImagePath ?? string.Empty),
                        character.ImagePath ?? string.Empty,
                        PickShortestTitle(character.AnimeTitles),
                        PickShortestTitle(character.MangaTitles)
                    );
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении персонажа по ID: {Id}", id);
            }
        }

        return result;
    }

    /// <summary>Состояние рейт-лимитера для админ-панели.</summary>
    public ShikimoriRateLimiterInfo GetRateLimiterInfo()
    {
        var info = _rateLimiter.GetInfo();

        return new ShikimoriRateLimiterInfo(
            info.AvailablePerSecond,
            info.AvailablePerMinute,
            info.TimeToResetSecond.TotalSeconds,
            info.TimeToResetMinute.TotalSeconds
        );
    }

    /// <summary>
    /// Короткое название из списка. null, если список пуст или все названия
    /// пустые: пустое имя показывать нечем.
    /// </summary>
    private static string? PickShortestTitle(IEnumerable<string?>? titles)
    {
        if (titles is null)
        {
            return null;
        }

        var shortest = titles
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => new { Title = title!, Length = title!.Length })
            .OrderBy(title => title.Length)
            .FirstOrDefault();

        return shortest?.Title;
    }

    private string BuildUrl(string path)
    {
        return $"{_options.ShikimoriSite.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
