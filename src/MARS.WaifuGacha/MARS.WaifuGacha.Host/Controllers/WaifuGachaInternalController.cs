using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using MARS.Twitch.Infrastructure.Http;
using MARS.WaifuGacha.Application.Models;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using MARS.WaifuGacha.Infrastructure.Postgres.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MikuTrackDto = MARS.Shared.Contracts.Models.MikuTrackDto;

namespace MARS.WaifuGacha.Controllers;

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
    MikuMondayService mikuMondayService,
    WaifuRollService waifuRollService,
    MikuRollService mikuRollService,
    FumoRollService fumoRollService,
    FrogRollService frogRollService,
    RollCooldownService cooldownService,
    RollCooldownConfigurationService cooldownConfiguration,
    RollCooldownNotificationState notificationState,
    MergeWaifuService mergeWaifuService,
    AddNewWaifuService addNewWaifuService,
    IShikimoriApiClient shikimoriClient,
    IDbContextFactory<WaifuDbContext> dbFactory,
    ITwitchUsersClient twitchUsersClient,
    ILogger<WaifuGachaInternalController> logger
) : ControllerBase
{
    private readonly ITwitchUsersClient _twitchUsersClient = twitchUsersClient;
    private readonly RollCooldownService _cooldownService = cooldownService;
    private readonly RollCooldownConfigurationService _cooldownConfiguration =
        cooldownConfiguration;
    private readonly RollCooldownNotificationState _notificationState = notificationState;

    /// <summary>
    /// Имя супруга пользователя — используется мини-игрой «Русская рулетка»,
    /// чтобы сообщить, кто именно спас игрока.
    /// </summary>
    [HttpGet("waifugacha/private-husbands")]
    public async Task<ActionResult<OperationResult<List<Husband>>>> GetPrivateHusbands(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var husbands = await db
                .Husbands.AsNoTracking()
                .Where(h => h.IsPrivated && h.WhenPrivated != null)
                .ToListAsync(cancellationToken);
            return Ok(OperationResult<List<Husband>>.Ok(husbands));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка");
            return Ok(OperationResult<List<Husband>>.Fail("Ошибка при получении супругов"));
        }
    }

    [HttpGet("waifugacha/has-fumo-user/{twitchUserId}")]
    public async Task<ActionResult<OperationResult<bool>>> HasFumoUser(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var has = await db.FumoRollUsers.AnyAsync(
                u => u.TwitchId == twitchUserId,
                cancellationToken
            );
            return Ok(OperationResult<bool>.Ok(has));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка");
            return Ok(OperationResult<bool>.Fail("Ошибка при проверке фумо"));
        }
    }

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
    /// Доступные треки Miku Monday для текущей недели.
    /// </summary>
    [HttpGet("miku-monday/tracks")]
    public async Task<ActionResult<OperationResult<List<MikuTrackDto>>>> GetMikuMondayTracks(
        CancellationToken cancellationToken = default
    )
    {
        var tracks = await mikuMondayService.GetAvailableTracksAsync(cancellationToken);
        return Ok(OperationResult<List<MikuTrackDto>>.Ok(tracks));
    }

    /// <summary>
    /// Выбор трека Miku Monday: для стримера без списания, для пользователя —
    /// с записью недельной активации.
    /// </summary>
    [HttpPost("miku-monday/tracks/random")]
    public async Task<ActionResult<OperationResult<MikuMondayResult>>> PickMikuMondayTrack(
        [FromBody] MikuMondayPickRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            return Ok(OperationResult<MikuMondayResult>.Fail("Запрос не передан"));
        }

        MikuMondayResult result;
        if (request.IsStreamer)
        {
            result = await mikuMondayService.GetRandomTrackForStreamerAsync(cancellationToken);
        }
        else
        {
            result = await mikuMondayService.GetRandomTrackForUserAsync(
                request.TwitchUserId,
                request.DisplayName,
                cancellationToken
            );
        }

        return Ok(OperationResult<MikuMondayResult>.Ok(result));
    }

    /// <summary>
    /// Вайфу-ролл по нику хоста (для команды !rollwaifu).
    /// </summary>
    [HttpPost("rolls/waifu")]
    public async Task<ActionResult<OperationResult<string>>> RollWaifu(
        [FromBody] WaifuRollRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.HostName))
        {
            return Ok(OperationResult<string>.Fail("Имя хоста не передано"));
        }

        var result = await waifuRollService.TelegramRollWaifu(request.HostName);
        if (!result.Success || result.Result?.Waifu is null)
        {
            return Ok(
                OperationResult<string>.Fail(
                    result.ErrorMessage ?? "Не удалось выполнить вайфу-ролл"
                )
            );
        }

        var hostName = result.Result.Host?.TwitchId ?? request.HostName;
        return Ok(OperationResult<string>.Ok($"Вайфу-ролл для {hostName} выполнен"));
    }

    /// <summary>
    /// Ролл костюма Мику (для команды !rollmiku).
    /// </summary>
    [HttpPost("rolls/miku")]
    public async Task<ActionResult<OperationResult<string>>> RollMiku(
        CancellationToken cancellationToken = default
    )
    {
        var module = await mikuRollService.RollTheMiku();
        return Ok(
            module is null
                ? OperationResult<string>.Fail("Не удалось найти костюм Miku")
                : OperationResult<string>.Ok($"Miku roll: {module.Title}")
        );
    }

    /// <summary>
    /// Ролл фумо (для команды !rollfumo).
    /// </summary>
    [HttpPost("rolls/fumo")]
    public async Task<ActionResult<OperationResult<string>>> RollFumo(
        CancellationToken cancellationToken = default
    )
    {
        var fumo = await fumoRollService.RollTheFumo();
        return Ok(
            fumo is null
                ? OperationResult<string>.Fail("Не удалось найти фумо")
                : OperationResult<string>.Ok($"Fumo roll: {fumo.Name}")
        );
    }

    /// <summary>
    /// Ролл лягушки (для команды !rollfrog).
    /// </summary>
    [HttpPost("rolls/frog")]
    public async Task<ActionResult<OperationResult<string>>> RollFrog(
        CancellationToken cancellationToken = default
    )
    {
        var frog = await frogRollService.RollTheFrog();
        return Ok(
            frog is null
                ? OperationResult<string>.Fail("Не удалось найти лягушку")
                : OperationResult<string>.Ok($"Frog roll: {frog.CommonName}")
        );
    }

    /// <summary>
    /// Объединение супруга (для reward !waifuunmerge — обратное).
    /// </summary>
    [HttpPost("husbands/{twitchId}/merge")]
    public async Task<ActionResult<OperationResult<string>>> MergeHusband(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await mergeWaifuService.MergeWaifuAsync(twitchId);
        return Ok(
            result.Success
                ? OperationResult<string>.Ok(result.ErrorMessage ?? "Объединение выполнено")
                : OperationResult<string>.Fail(result.ErrorMessage ?? "Не удалось объединить")
        );
    }

    /// <summary>
    /// Ролл лягушки для оверлея: возвращает сущность лягушки в JSON.
    /// </summary>
    [HttpPost("rolls/frog/full")]
    public async Task<ActionResult<OperationResult<FrogRollResultDto>>> RollFrogFull(
        [FromQuery] string? twitchUserId = null,
        CancellationToken cancellationToken = default
    )
    {
        var cooldown = await _cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.FrogRollType,
            cancellationToken
        );
        var (allowed, remaining) = await _cooldownService.CheckAndUpdateCooldownAsync(
            twitchUserId ?? string.Empty,
            RootStateKeys.FrogRollType,
            cooldown,
            cancellationToken
        );
        if (!allowed)
        {
            return Ok(
                OperationResult<FrogRollResultDto>.Ok(
                    new FrogRollResultDto(null, true, (int)remaining.TotalSeconds)
                )
            );
        }

        await _notificationState.AddPendingAsync(
            twitchUserId ?? string.Empty,
            RootStateKeys.FrogRollType,
            cooldown,
            cancellationToken
        );

        var frog = await frogRollService.RollTheFrog();
        if (frog is null)
        {
            return Ok(OperationResult<FrogRollResultDto>.Fail("Не удалось найти лягушку"));
        }

        return Ok(OperationResult<FrogRollResultDto>.Ok(new FrogRollResultDto(frog, false, 0)));
    }

    /// <summary>
    /// Ролл фумо для оверлея: сущность + счётчики коллекции.
    /// </summary>
    [HttpPost("rolls/fumo/full")]
    public async Task<ActionResult<OperationResult<FumoRollResultDto>>> RollFumoFull(
        [FromQuery] string? twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var cooldown = await _cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.FumoRollType,
            cancellationToken
        );
        var (allowed, remaining) = await _cooldownService.CheckAndUpdateCooldownAsync(
            twitchUserId ?? string.Empty,
            RootStateKeys.FumoRollType,
            cooldown,
            cancellationToken
        );
        if (!allowed)
        {
            return Ok(
                OperationResult<FumoRollResultDto>.Ok(
                    new FumoRollResultDto(null, 0, 0, true, (int)remaining.TotalSeconds)
                )
            );
        }

        await _notificationState.AddPendingAsync(
            twitchUserId ?? string.Empty,
            RootStateKeys.FumoRollType,
            cooldown,
            cancellationToken
        );

        var fumo = await fumoRollService.RollTheFumo();
        if (fumo is null)
        {
            return Ok(OperationResult<FumoRollResultDto>.Fail("Не удалось найти фумо"));
        }

        var stats = await fumoCollectionService.RecordRollAsync(
            twitchUserId ?? string.Empty,
            fumo.MfcId
        );

        return Ok(
            OperationResult<FumoRollResultDto>.Ok(
                new FumoRollResultDto(fumo, stats.CollectedCount, stats.TotalCount, false, 0)
            )
        );
    }

    /// <summary>
    /// Ролл мику для оверлея: сущность + счётчики коллекции.
    /// </summary>
    [HttpPost("rolls/miku/full")]
    public async Task<ActionResult<OperationResult<MikuRollResultDto>>> RollMikuFull(
        [FromQuery] string? twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var cooldown = await _cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.MikuRollType,
            cancellationToken
        );
        var (allowed, remaining) = await _cooldownService.CheckAndUpdateCooldownAsync(
            twitchUserId ?? string.Empty,
            RootStateKeys.MikuRollType,
            cooldown,
            cancellationToken
        );
        if (!allowed)
        {
            return Ok(
                OperationResult<MikuRollResultDto>.Ok(
                    new MikuRollResultDto(null, 0, 0, true, (int)remaining.TotalSeconds)
                )
            );
        }

        await _notificationState.AddPendingAsync(
            twitchUserId ?? string.Empty,
            RootStateKeys.MikuRollType,
            cooldown,
            cancellationToken
        );

        var module = await mikuRollService.RollTheMiku();
        if (module is null)
        {
            return Ok(OperationResult<MikuRollResultDto>.Fail("Не удалось найти костюм Miku"));
        }

        var stats = await mikuCollectionService.RecordRollAsync(
            twitchUserId ?? string.Empty,
            module.PageId
        );

        return Ok(
            OperationResult<MikuRollResultDto>.Ok(
                new MikuRollResultDto(module, stats.CollectedCount, stats.TotalCount, false, 0)
            )
        );
    }

    /// <summary>
    /// Вайфу-ролл для оверлея: сущность вайфу + хост.
    /// </summary>
    [HttpPost("rolls/waifu/full")]
    public async Task<ActionResult<OperationResult<WaifuRollResultDto>>> RollWaifuFull(
        [FromQuery] string hostName,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return Ok(OperationResult<WaifuRollResultDto>.Fail("Имя хоста не передано"));
        }

        var result = await waifuRollService.TelegramRollWaifu(hostName);
        if (!result.Success || result.Result?.Waifu is null)
        {
            var host = result.Result?.Host;
            var cooldownTime = host?.HusbandCoolDown?.Time;
            var remaining = TimeSpan.Zero;
            if (cooldownTime.HasValue && cooldownTime.Value != default)
            {
                var cooldownDuration = await waifuRollService.GetWaifuRollCoolDownAsync(
                    cancellationToken
                );
                var elapsed = DateTime.UtcNow - cooldownTime.Value;
                if (elapsed < cooldownDuration)
                {
                    remaining = cooldownDuration - elapsed;
                }
            }

            if (remaining > TimeSpan.Zero)
            {
                return Ok(
                    OperationResult<WaifuRollResultDto>.Ok(
                        new WaifuRollResultDto(null, host, true, (int)remaining.TotalSeconds)
                    )
                );
            }

            return Ok(
                OperationResult<WaifuRollResultDto>.Fail(
                    result.ErrorMessage ?? "Не удалось выполнить вайфу-ролл"
                )
            );
        }

        var r = result.Result!;
        if (r.Host is not null && r.Host.TwitchUser is null)
        {
            var restUser = await _twitchUsersClient.GetFullUserByLoginAsync(r.Host.TwitchId);
            if (restUser is not null)
            {
                r.Host.TwitchUser = new MARS.Shared.Kernel.TwitchUser
                {
                    TwitchId = restUser.TwitchId,
                    UserLogin = restUser.UserLogin,
                    DisplayName = restUser.DisplayName,
                    ProfileImageUrl = restUser.ProfileImageUrl,
                    ChatColor = restUser.ChatColor,
                    IsModerator = restUser.IsModerator,
                    IsVip = restUser.IsVip,
                };
            }
        }
        if (r.Host is not null && !string.IsNullOrWhiteSpace(r.Host.TwitchId))
        {
            var cooldownDuration = await waifuRollService.GetWaifuRollCoolDownAsync(
                cancellationToken
            );
            await _notificationState.AddPendingAsync(
                r.Host.TwitchId,
                RootStateKeys.WaifuRollType,
                cooldownDuration,
                cancellationToken
            );
        }
        return Ok(
            OperationResult<WaifuRollResultDto>.Ok(
                new WaifuRollResultDto(r.Waifu, r.Host, false, 0)
            )
        );
    }

    /// <summary>
    /// Объединение супруга для оверлея.
    /// </summary>
    [HttpPost("husbands/{twitchId}/merge-full")]
    public async Task<ActionResult<OperationResult<MergeWaifuResultDto>>> MergeHusbandFull(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await mergeWaifuService.MergeWaifuAsync(twitchId);
        if (!result.Success || result.Result is null)
        {
            return Ok(
                OperationResult<MergeWaifuResultDto>.Fail(
                    result.ErrorMessage ?? "Не удалось объединить"
                )
            );
        }

        var r = result.Result;
        if (r.Host is not null && r.Host.TwitchUser is null)
        {
            var restUser = await _twitchUsersClient.GetFullUserByLoginAsync(r.Host.TwitchId);
            if (restUser is not null)
            {
                r.Host.TwitchUser = new MARS.Shared.Kernel.TwitchUser
                {
                    TwitchId = restUser.TwitchId,
                    UserLogin = restUser.UserLogin,
                    DisplayName = restUser.DisplayName,
                    ProfileImageUrl = restUser.ProfileImageUrl,
                    ChatColor = restUser.ChatColor,
                    IsModerator = restUser.IsModerator,
                    IsVip = restUser.IsVip,
                };
            }
        }
        return Ok(
            OperationResult<MergeWaifuResultDto>.Ok(
                new MergeWaifuResultDto(r.Waifu, r.Host, r.IsNewMarriage)
            )
        );
    }

    /// <summary>
    /// Добавление новой вайфу для оверлея.
    /// </summary>
    [HttpPost("waifus/add")]
    public async Task<ActionResult<OperationResult<AddNewWaifuResultDto>>> AddNewWaifuFull(
        [FromQuery] string userInput,
        [FromQuery] string userId,
        [FromQuery] string displayName,
        [FromQuery] bool isVip,
        CancellationToken cancellationToken = default
    )
    {
        var result = await addNewWaifuService.AddNewWaifuAsync(
            userInput,
            userId,
            displayName,
            isVip
        );
        if (!result.Success || result.Result?.Waifu is null)
        {
            return Ok(
                OperationResult<AddNewWaifuResultDto>.Fail(
                    result.ErrorMessage ?? "Не удалось добавить супруга"
                )
            );
        }

        var r = result.Result;
        return Ok(
            OperationResult<AddNewWaifuResultDto>.Ok(
                new AddNewWaifuResultDto(
                    r.Waifu,
                    r.DisplayName ?? displayName,
                    "#ffffff",
                    r.VipDropped,
                    r.RollsUntilGuarantee
                )
            )
        );
    }

    [HttpPost("husbands/{twitchId}/unmerge")]
    public async Task<ActionResult<OperationResult<string>>> UnmergeHusband(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var host = await db.Husbands.FirstOrDefaultAsync(
                h => h.TwitchId == twitchId,
                cancellationToken
            );

            if (host is not { IsPrivated: true })
            {
                return Ok(OperationResult<string>.Fail("Муж не найден или не в браке"));
            }

            var waifu = await db.Waifus.FirstOrDefaultAsync(
                w => w.ShikiId == host.WaifuBrideId,
                cancellationToken
            );

            if (waifu is not { IsPrivated: true })
            {
                return Ok(
                    OperationResult<string>.Fail(
                        $"Не удалось найти вайфу этого мужа ({host.TwitchId})"
                    )
                );
            }

            waifu.IsPrivated = false;
            host.WaifuBrideId = null;
            host.IsPrivated = false;
            host.WhenPrivated = null;

            db.Waifus.Update(waifu);
            db.Husbands.Update(host);
            await db.SaveChangesAsync(cancellationToken);

            return Ok(OperationResult<string>.Ok($"Развод выполнен: {waifu.Name}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unmerging husband with TwitchId: {TwitchId}", twitchId);
            return Ok(OperationResult<string>.Fail("Ошибка при разводе"));
        }
    }
}
