using MARS.Shikimori.Application.Models;
using MARS.Shikimori.Application.Ports;
using MARS.Shikimori.Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShikimoriSharp;
using ShikimoriSharp.Bases;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Enums;
using ShikimoriSharp.Settings;

namespace MARS.Shikimori.Infrastructure.Clients;

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
