using MARS.Shared.Clients;
using MARS.Shikimori.Data;
using Microsoft.Extensions.Options;
using ShikimoriSharp;
using ShikimoriSharp.Bases;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Settings;

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
/// </remarks>
public class ShikimoriService
{
    /// <summary>Порог оценки для случайных произведений — из монолита.</summary>
    private const int MinimumScore = 7;

    private readonly ILogger<ShikimoriService> _logger;
    private readonly IShikimoriRateLimiter _rateLimiter;
    private readonly ShikimoriClientOptions _options;
    private readonly ShikimoriSharp.ShikimoriClient _client;

    public ShikimoriService(
        ILogger<ShikimoriService> logger,
        IOptions<ShikimoriClientOptions> configuration,
        IShikimoriRateLimiter rateLimiter
    )
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
        _options = configuration.Value;
        _client = new ShikimoriSharp.ShikimoriClient(
            logger,
            new ClientSettings(_options.ClientName, _options.ClientId, _options.ClientSecret)
        );
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

            var animes = await _client.Animes.GetAnime(
                new AnimeRequestSettings
                {
                    order = ShikimoriSharp.Enums.Order.random,
                    limit = 1,
                    score = MinimumScore,
                }
            );

            var anime = animes?.FirstOrDefault();

            if (anime is not null)
            {
                result = new ShikimoriTitleRef(
                    anime.Id,
                    anime.Name,
                    anime.Russian,
                    anime.AiredOn?.Year,
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

            var mangas = await _client.Mangas.GetBySearch(
                new MangaRequestSettings
                {
                    order = ShikimoriSharp.Enums.Order.random,
                    limit = 1,
                    score = MinimumScore,
                }
            );

            var manga = mangas?.FirstOrDefault();

            if (manga is not null)
            {
                result = new ShikimoriTitleRef(
                    manga.Id,
                    manga.Name,
                    manga.Russian,
                    manga.AiredOn?.Year,
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

                var character = await _client.Characters.GetCharacterById(id);

                if (character is not null)
                {
                    result = new ShikimoriCharacterRef(
                        character.Id,
                        character.Name ?? character.Russian ?? "Unknown",
                        character.Russian,
                        string.IsNullOrWhiteSpace(character.Description)
                            ? null
                            : character.Description,
                        BuildUrl(character.Image?.Original ?? string.Empty),
                        character.Image?.Original ?? string.Empty,
                        PickShortestTitle(
                            character.Animes?.Select(anime => anime.Russian ?? anime.Name)
                        ),
                        PickShortestTitle(
                            character.Mangas?.Select(manga => manga.Russian ?? manga.Name)
                        )
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
