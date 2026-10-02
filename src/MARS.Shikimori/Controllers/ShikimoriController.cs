using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.Shikimori.Services;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Shikimori.Controllers;

/// <summary>
/// Внутренний API Shikimori. Единственный адрес, по которому Shikimori
/// доступен остальным сервисам: клиент и рейт-лимитер живут в
/// <c>MARS.Shikimori</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShikimoriController(
    ShikimoriService shikimoriService,
    ShikimoriCatalog catalog,
    ILogger<ShikimoriController> logger
) : ControllerBase
{
    /// <summary>Случайное аниме с оценкой не ниже 7.</summary>
    [HttpGet("random-anime")]
    public async Task<ActionResult<OperationResult<ShikimoriTitleRef>>> GetRandomAnime(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriTitleRef>> result;

        var anime = await shikimoriService.GetRandomAnimeAsync(cancellationToken);

        if (anime is null)
        {
            result = Ok(OperationResult<ShikimoriTitleRef>.Fail("Shikimori не вернул аниме"));
        }
        else
        {
            await catalog.RecordPickAsync("anime", anime, cancellationToken);
            result = Ok(OperationResult<ShikimoriTitleRef>.Ok(anime));
        }

        return result;
    }

    /// <summary>Случайная манга с оценкой не ниже 7.</summary>
    [HttpGet("random-manga")]
    public async Task<ActionResult<OperationResult<ShikimoriTitleRef>>> GetRandomManga(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriTitleRef>> result;

        var manga = await shikimoriService.GetRandomMangaAsync(cancellationToken);

        if (manga is null)
        {
            result = Ok(OperationResult<ShikimoriTitleRef>.Fail("Shikimori не вернул мангу"));
        }
        else
        {
            await catalog.RecordPickAsync("manga", manga, cancellationToken);
            result = Ok(OperationResult<ShikimoriTitleRef>.Ok(manga));
        }

        return result;
    }

    /// <summary>
    /// Персонаж по id. Данные берутся у Shikimori свежими и тут же
    /// сохраняются: иначе история персонажей в базе не наполнялась бы.
    /// </summary>
    [HttpGet("characters/{id:long}")]
    public async Task<ActionResult<OperationResult<ShikimoriCharacterRef>>> GetCharacter(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriCharacterRef>> result;

        if (id <= 0)
        {
            result = Ok(
                OperationResult<ShikimoriCharacterRef>.Fail(
                    "id персонажа должен быть положительным"
                )
            );
        }
        else
        {
            var character = await shikimoriService.GetCharacterAsync(id, cancellationToken);

            if (character is null)
            {
                result = Ok(
                    OperationResult<ShikimoriCharacterRef>.Fail(
                        $"Shikimori не вернул персонажа {id}"
                    )
                );
            }
            else
            {
                var saved = await catalog.SaveCharacterAsync(character, cancellationToken);
                result = Ok(OperationResult<ShikimoriCharacterRef>.Ok(saved));
            }
        }

        return result;
    }

    /// <summary>
    /// Персонаж из локальной базы, без обращения к Shikimori и без расхода
    /// рейт-лимитера.
    /// </summary>
    [HttpGet("characters/{id:long}/cached")]
    public async Task<ActionResult<OperationResult<ShikimoriCharacterRef>>> GetCachedCharacter(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriCharacterRef>> result;

        var character = await catalog.FindCharacterAsync(id, cancellationToken);

        if (character is null)
        {
            logger.LogDebug("Персонаж {CharacterId} ещё не сохранён в базе", id);
            result = Ok(
                OperationResult<ShikimoriCharacterRef>.Fail($"Персонаж {id} ещё не сохранён")
            );
        }
        else
        {
            result = Ok(OperationResult<ShikimoriCharacterRef>.Ok(character));
        }

        return result;
    }

    /// <summary>Состояние рейт-лимитера: его видит админ-панель.</summary>
    [HttpGet("rate-limiter")]
    public ActionResult<OperationResult<ShikimoriRateLimiterInfo>> GetRateLimiterInfo()
    {
        return Ok(
            OperationResult<ShikimoriRateLimiterInfo>.Ok(shikimoriService.GetRateLimiterInfo())
        );
    }
}
