using MARS.Shared.Clients;
using MARS.Shikimori.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShikimoriSharp;
using ShikimoriSharp.Bases;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Enums;
using ShikimoriSharp.Settings;

namespace MARS.Shikimori.Services;

/// <summary>
/// Клиент Shikimori: единственное место, где живёт <c>ShikimoriSharp</c>.
///
/// Вынесен в интерфейс, потому что настоящий клиент ходит в сеть, а проверять
/// нужно и разбор ответа, и правило «самое короткое название», и рейт-лимит.
/// Без разрыва тест проверял бы доступность Shikimori, а не код: при недоступном
/// внешнем API он проходил бы, ничего не проверив, а в CI без сети падал бы.
///
/// Модель та же, что у <c>IMediaCompressor</c>/<c>IFfmpegRunner</c> и у
/// <c>IYouTubeResolver</c> в MARS.Discord.
/// </summary>
public interface IShikimoriClient
{
    /// <summary>Случайное аниме с оценкой не ниже <paramref name="minimumScore"/>.</summary>
    Task<AnimeRef?> GetRandomAnimeAsync(
        int minimumScore,
        CancellationToken cancellationToken = default
    );

    /// <summary>Случайная манга с оценкой не ниже <paramref name="minimumScore"/>.</summary>
    Task<MangaRef?> GetRandomMangaAsync(
        int minimumScore,
        CancellationToken cancellationToken = default
    );

    /// <summary>Персонаж по id вместе с его произведениями.</summary>
    Task<CharacterDetails?> GetCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    );
}

/// <summary>Аниме в доменном виде — без узлов GraphQL.</summary>
public record AnimeRef(long Id, string? Name, string? Russian, int? Year);

/// <summary>Манга в доменном виде.</summary>
public record MangaRef(long Id, string? Name, string? Russian, int? Year);

/// <summary>
/// Персонаж в доменном виде. Названия произведений приходят строками — выбор
/// самого короткого остаётся на стороне <see cref="ShikimoriService"/>, где это
/// правило и записано.
/// </summary>
public record CharacterDetails(
    long Id,
    string Name,
    string? Russian,
    string? Description,
    string? ImagePath,
    IReadOnlyList<string?> AnimeTitles,
    IReadOnlyList<string?> MangaTitles
);

/// <inheritdoc />
public sealed class ShikimoriSharpClient : IShikimoriClient
{
    // Токен из интерфейса в ShikimoriSharp не передаётся: там у методов вторым
    // параметром идёт AccessToken, а не CancellationToken. Отмена применяется
    // вызывающим кодом.
    private readonly ILogger<ShikimoriSharpClient> _logger;
    private readonly ShikimoriSharp.ShikimoriClient _client;

    public ShikimoriSharpClient(
        ILogger<ShikimoriSharpClient> logger,
        IOptions<ShikimoriClientOptions> configuration
    )
    {
        _logger = logger;
        var options = configuration.Value;
        _client = new ShikimoriSharp.ShikimoriClient(
            logger,
            new ClientSettings(options.ClientName, options.ClientId, options.ClientSecret)
        );
    }

    public async Task<AnimeRef?> GetRandomAnimeAsync(
        int minimumScore,
        CancellationToken cancellationToken = default
    )
    {
        var animes = await _client.Animes.GetAnime(
            new AnimeRequestSettings
            {
                order = Order.random,
                limit = 1,
                score = minimumScore,
            }
        );

        var anime = animes?.FirstOrDefault();

        return anime is null
            ? null
            : new AnimeRef(anime.Id, anime.Name, anime.Russian, anime.AiredOn?.Year);
    }

    public async Task<MangaRef?> GetRandomMangaAsync(
        int minimumScore,
        CancellationToken cancellationToken = default
    )
    {
        var mangas = await _client.Mangas.GetBySearch(
            new MangaRequestSettings
            {
                order = Order.random,
                limit = 1,
                score = minimumScore,
            }
        );

        var manga = mangas?.FirstOrDefault();

        return manga is null
            ? null
            : new MangaRef(manga.Id, manga.Name, manga.Russian, manga.AiredOn?.Year);
    }

    public async Task<CharacterDetails?> GetCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        var character = await _client.Characters.GetCharacterById(id);

        if (character is null)
        {
            _logger.LogWarning("Shikimori не вернул персонажа {Id}", id);
            return null;
        }

        return new CharacterDetails(
            character.Id,
            character.Name ?? character.Russian ?? "Unknown",
            character.Russian,
            string.IsNullOrWhiteSpace(character.Description) ? null : character.Description,
            character.Image?.Original,
            character.Animes?.Select(anime => anime.Russian ?? anime.Name).ToArray() ?? [],
            character.Mangas?.Select(manga => manga.Russian ?? manga.Name).ToArray() ?? []
        );
    }
}
