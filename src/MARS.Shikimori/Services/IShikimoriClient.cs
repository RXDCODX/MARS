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
