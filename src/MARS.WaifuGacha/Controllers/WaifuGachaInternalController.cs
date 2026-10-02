using MARS.Shared.Clients;
using MARS.Shared.Extensions;
using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record AutoHelloRequest
{
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Внутренние межсервисные эндпоинты MARS.WaifuGacha.
/// Доступны только с корректным <c>X-Api-Key</c> (политика <c>ServiceApiKey</c>),
/// в отличие от публичного <see cref="WaifuRollController"/>, который нужен UI-клиенту.
/// Закрывают зависимости MARS.TwitchCore, которым раньше не хватало реализаций:
/// <c>IAutoHelloService</c> и <c>IWaifuLookupService</c>.
/// </summary>
[ApiController]
[Route("api/internal")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class WaifuGachaInternalController(
    AutoHelloService autoHelloService,
    FumoCollectionService fumoCollectionService,
    MikuCollectionService mikuCollectionService,
    IShikimoriApiClient shikimoriClient,
    IDbContextFactory<WaifuDbContext> dbFactory,
    ILogger<WaifuGachaInternalController> logger
) : ControllerBase
{
    /// <summary>
    /// Имя супруга пользователя — используется мини-игрой «Русская рулетка»,
    /// чтобы сообщить, кто именно спас игрока.
    /// </summary>
    [HttpGet("husbands/{twitchId}/waifu-name")]
    public async Task<ActionResult<OperationResult<string?>>> GetWaifuName(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string?>> result = null!;

        try
        {
            if (string.IsNullOrWhiteSpace(twitchId))
            {
                result = Ok(OperationResult<string?>.Fail("Twitch ID не передан"));
            }
            else
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

                var name = await db
                    .Husbands.AsNoTracking()
                    .Where(h => h.TwitchId == twitchId && h.IsPrivated)
                    .Where(h => h.WaifuBrideId != null)
                    .Join(
                        db.Waifus.AsNoTracking(),
                        h => h.WaifuBrideId,
                        w => w.ShikiId,
                        (_, w) => w.Name
                    )
                    .FirstOrDefaultAsync(cancellationToken);

                result = Ok(
                    OperationResult<string?>.Ok(string.IsNullOrWhiteSpace(name) ? null : name)
                );
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ошибка при получении имени супруга для {TwitchId}",
                twitchId
            );
            result = Ok(OperationResult<string?>.Fail("Ошибка при получении имени супруга"));
        }

        return result;
    }

    /// <summary>
    /// Текст авто-приветствия от супруга. Публикуется только когда пользователь
    /// в браке, приветствия включены и с прошлого прошло больше 20 часов.
    /// </summary>
    [HttpPost("auto-hello/{twitchId}")]
    public async Task<ActionResult<OperationResult<string?>>> GetAutoHello(
        string twitchId,
        [FromBody] AutoHelloRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<string?>> result = null!;

        try
        {
            if (string.IsNullOrWhiteSpace(twitchId) || request is null)
            {
                result = Ok(
                    OperationResult<string?>.Fail("Не переданы twitch ID или имя пользователя")
                );
            }
            else
            {
                var message = await autoHelloService.GetAutoHelloMessageAsync(
                    twitchId,
                    request.DisplayName,
                    cancellationToken
                );

                result = Ok(OperationResult<string?>.Ok(message));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при auto-hello для {TwitchId}", twitchId);
            result = Ok(OperationResult<string?>.Fail("Ошибка при получении auto-hello"));
        }

        return result;
    }

    /// <summary>
    /// Переключает auto-hello для пользователя и возвращает новое состояние.
    /// Закрывает функциональность чат-команды <c>!autohello</c> из монолита.
    /// </summary>
    [HttpPost("auto-hello/{twitchId}/toggle")]
    public async Task<ActionResult<OperationResult<bool>>> ToggleAutoHello(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<bool>> result = null!;

        try
        {
            if (string.IsNullOrWhiteSpace(twitchId))
            {
                result = Ok(OperationResult<bool>.Fail("Twitch ID не передан"));
            }
            else
            {
                var enabled = await autoHelloService.ToggleAutoHelloEnabledAsync(
                    twitchId,
                    cancellationToken
                );

                result = Ok(OperationResult<bool>.Ok(enabled));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ошибка при переключении auto-hello для {TwitchId}",
                twitchId
            );
            result = Ok(OperationResult<bool>.Fail("Ошибка при переключении auto-hello"));
        }

        return result;
    }

    /// <summary>
    /// Инвентарь собранных фумо. Один эндпоинт на коллекцию: различаются
    /// только имя сервиса-запроса, а коллекций впереди может быть больше.
    /// </summary>
    [HttpGet("collections/fumo")]
    public async Task<ActionResult<OperationResult<CollectionInventory>>> GetFumoInventory(
        [FromQuery] string twitchId
    )
    {
        var result = await fumoCollectionService.GetInventoryAsync(
            twitchId,
            HttpContext.RequestAborted
        );

        return Ok(ToEnvelope(result));
    }

    /// <summary>
    /// Инвентарь собранных мику-модулей.
    /// </summary>
    [HttpGet("collections/miku")]
    public async Task<ActionResult<OperationResult<CollectionInventory>>> GetMikuInventory(
        [FromQuery] string twitchId
    )
    {
        var result = await mikuCollectionService.GetInventoryAsync(
            twitchId,
            HttpContext.RequestAborted
        );

        return Ok(ToEnvelope(result));
    }

    /// <summary>
    /// Случайное аниме с Shikimori. Ссылку собирает MARS.Shikimori:
    /// <c>randomanime</c> не знает адреса сайта и не должна знать его.
    /// </summary>
    [HttpGet("shikimori/random-anime")]
    public async Task<ActionResult<OperationResult<ShikimoriTitleRef>>> GetRandomAnime(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriTitleRef>> result = null!;

        var anime = await shikimoriClient.GetRandomAnimeAsync(cancellationToken);

        result = anime is null
            ? Ok(OperationResult<ShikimoriTitleRef>.Fail("Shikimori не вернул аниме"))
            : Ok(OperationResult<ShikimoriTitleRef>.Ok(anime));

        return result;
    }

    /// <summary>
    /// Случайная манга с Shikimori.
    /// </summary>
    [HttpGet("shikimori/random-manga")]
    public async Task<ActionResult<OperationResult<ShikimoriTitleRef>>> GetRandomManga(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriTitleRef>> result = null!;

        var manga = await shikimoriClient.GetRandomMangaAsync(cancellationToken);

        result = manga is null
            ? Ok(OperationResult<ShikimoriTitleRef>.Fail("Shikimori не вернул мангу"))
            : Ok(OperationResult<ShikimoriTitleRef>.Ok(manga));

        return result;
    }

    private static OperationResult<CollectionInventory> ToEnvelope(
        OperationResult<CollectionInventory> result
    )
    {
        return result.Success && result.Result is not null
            ? OperationResult<CollectionInventory>.Ok(result.Result)
            : OperationResult<CollectionInventory>.Fail(
                result.ErrorMessage ?? "Не удалось прочитать инвентарь"
            );
    }

    /// <summary>
    /// конкатенацией, и двойной слэш после домена ломал бы некоторые ссылки.
    /// </summary>
}
