using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Controllers;

public record WaifuDto
{
    public string ShikiId { get; init; } = "";
    public string Name { get; init; } = "";
    public long Age { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
    public DateTime WhenAdded { get; init; }
    public DateTime LastOrder { get; init; }
    public int OrderCount { get; init; }
    public bool IsPrivated { get; init; }
    public string ImageUrl { get; init; } = "";
    public Guid? AudioId { get; init; }
    public string? AudioName { get; init; }
}

public record CreateWaifuRequest
{
    public string ShikiId { get; init; } = "";
    public string Name { get; init; } = "";
    public long Age { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
    public string? ImageUrl { get; init; }
    public Guid? AudioId { get; init; }
}

public record UpdateWaifuRequest
{
    public string? Name { get; init; }
    public long? Age { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
    public string? ImageUrl { get; init; }
    public bool? IsPrivated { get; init; }
    public Guid? AudioId { get; init; }
}

public record HusbandDto
{
    public string TwitchId { get; init; } = "";
    public DateTime WhenOrdered { get; init; }
    public string? WaifuBrideId { get; init; }
    public bool IsPrivated { get; init; }
    public long OrderCount { get; init; }
    public string? WaifuRollId { get; init; }
    public DateTime? WhenPrivated { get; init; }
    public int? LastWeddingCongratulatedMonths { get; init; }
}

public record UpdateHusbandRequest
{
    public string? WaifuBrideId { get; init; }
    public bool? IsPrivated { get; init; }
    public string? WaifuRollId { get; init; }
    public DateTime? WhenPrivated { get; init; }
    public int? LastWeddingCongratulatedMonths { get; init; }
}

public record WaifuRollAudioDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string FileExtension { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}

[ApiController]
[Route("api/[controller]")]
public class WaifuRollController(
    IDbContextFactory<WaifuDbContext> dbFactory,
    IOptions<ShikimoriClientOptions> shikiOptions,
    ILogger<WaifuRollController> logger
) : ControllerBase
{
    private readonly string _shikimoriSite = shikiOptions.Value.ShikimoriSite ?? "";

    private static string FixImageUrl(string? imageUrl, string shikimoriSite)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return imageUrl ?? "";
        }
        return imageUrl.StartsWith(shikimoriSite, StringComparison.OrdinalIgnoreCase)
            ? imageUrl
            : shikimoriSite + imageUrl;
    }

    private static string NormalizeImageUrl(string? imageUrl, string shikimoriSite)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return imageUrl ?? "";
        }
        return imageUrl.StartsWith(shikimoriSite, StringComparison.OrdinalIgnoreCase)
            ? imageUrl[shikimoriSite.Length..]
            : imageUrl;
    }

    #region Waifu Endpoints

    [HttpGet("waifus")]
    public async Task<ActionResult<OperationResult<List<WaifuDto>>>> GetAllWaifus(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var waifus = await db
                .Waifus.AsNoTracking()
                .Include(w => w.Audio)
                .OrderByDescending(w => w.WhenAdded)
                .Select(w => new WaifuDto
                {
                    ShikiId = w.ShikiId,
                    Name = w.Name,
                    Age = w.Age,
                    Anime = w.Anime,
                    Manga = w.Manga,
                    WhenAdded = w.WhenAdded,
                    LastOrder = w.LastOrder,
                    OrderCount = w.OrderCount,
                    IsPrivated = w.IsPrivated,
                    ImageUrl = w.ImageUrl,
                    AudioId = w.AudioId,
                    AudioName = w.Audio != null ? w.Audio.Name : null,
                })
                .ToListAsync(cancellationToken);

            return Ok(OperationResult<List<WaifuDto>>.Ok(waifus));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting waifus");
            return Ok(OperationResult<List<WaifuDto>>.Fail("Ошибка при получении вайфу"));
        }
    }

    [HttpGet("waifus/{shikiId}")]
    public async Task<ActionResult<OperationResult<WaifuDto?>>> GetWaifu(
        string shikiId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var waifu = await db
                .Waifus.AsNoTracking()
                .Include(w => w.Audio)
                .Where(w => w.ShikiId == shikiId)
                .Select(w => new WaifuDto
                {
                    ShikiId = w.ShikiId,
                    Name = w.Name,
                    Age = w.Age,
                    Anime = w.Anime,
                    Manga = w.Manga,
                    WhenAdded = w.WhenAdded,
                    LastOrder = w.LastOrder,
                    OrderCount = w.OrderCount,
                    IsPrivated = w.IsPrivated,
                    ImageUrl = w.ImageUrl,
                    AudioId = w.AudioId,
                    AudioName = w.Audio != null ? w.Audio.Name : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            return waifu != null
                ? Ok(OperationResult<WaifuDto?>.Ok(waifu))
                : Ok(OperationResult<WaifuDto?>.Fail($"Вайфу с ID {shikiId} не найдена"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting waifu with ShikiId: {ShikiId}", shikiId);
            return Ok(OperationResult<WaifuDto?>.Fail("Ошибка при получении вайфу"));
        }
    }

    [HttpPost("waifus")]
    public async Task<ActionResult<OperationResult<WaifuDto?>>> CreateWaifu(
        CreateWaifuRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (request == null)
            {
                return Ok(OperationResult<WaifuDto?>.Fail("Тело запроса не может быть пустым"));
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var exists = await db
                .Waifus.AsNoTracking()
                .AnyAsync(w => w.ShikiId == request.ShikiId, cancellationToken);

            if (exists)
            {
                return Ok(
                    OperationResult<WaifuDto?>.Fail($"Вайфу с ID {request.ShikiId} уже существует")
                );
            }

            var waifu = new Waifu
            {
                ShikiId = request.ShikiId,
                Name = request.Name,
                Age = request.Age,
                Anime = request.Anime,
                Manga = request.Manga,
                ImageUrl = NormalizeImageUrl(request.ImageUrl, _shikimoriSite),
                AudioId = request.AudioId,
                WhenAdded = DateTime.Now,
                LastOrder = DateTime.MinValue,
            };

            db.Waifus.Add(waifu);
            await db.SaveChangesAsync(cancellationToken);

            var dto = new WaifuDto
            {
                ShikiId = waifu.ShikiId,
                Name = waifu.Name,
                Age = waifu.Age,
                Anime = waifu.Anime,
                Manga = waifu.Manga,
                WhenAdded = waifu.WhenAdded,
                LastOrder = waifu.LastOrder,
                OrderCount = waifu.OrderCount,
                IsPrivated = waifu.IsPrivated,
                ImageUrl = waifu.ImageUrl,
                AudioId = waifu.AudioId,
            };

            return Ok(OperationResult<WaifuDto?>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating waifu");
            return Ok(OperationResult<WaifuDto?>.Fail("Ошибка при создании вайфу"));
        }
    }

    [HttpPut("waifus/{shikiId}")]
    public async Task<ActionResult<OperationResult<WaifuDto?>>> UpdateWaifu(
        string shikiId,
        UpdateWaifuRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (request == null)
            {
                return Ok(OperationResult<WaifuDto?>.Fail("Тело запроса не может быть пустым"));
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var waifu = await db.Waifus.FirstOrDefaultAsync(
                w => w.ShikiId == shikiId,
                cancellationToken
            );

            if (waifu == null)
            {
                return Ok(OperationResult<WaifuDto?>.Fail($"Вайфу с ID {shikiId} не найдена"));
            }

            if (request.Name != null)
                waifu.Name = request.Name;
            if (request.Age.HasValue)
                waifu.Age = request.Age.Value;
            if (request.Anime != null)
                waifu.Anime = request.Anime;
            if (request.Manga != null)
                waifu.Manga = request.Manga;
            if (request.ImageUrl != null)
                waifu.ImageUrl = NormalizeImageUrl(request.ImageUrl, _shikimoriSite);
            if (request.IsPrivated.HasValue)
                waifu.IsPrivated = request.IsPrivated.Value;
            if (request.AudioId.HasValue || request.AudioId == null)
                waifu.AudioId = request.AudioId;

            await db.SaveChangesAsync(cancellationToken);

            var dto = new WaifuDto
            {
                ShikiId = waifu.ShikiId,
                Name = waifu.Name,
                Age = waifu.Age,
                Anime = waifu.Anime,
                Manga = waifu.Manga,
                WhenAdded = waifu.WhenAdded,
                LastOrder = waifu.LastOrder,
                OrderCount = waifu.OrderCount,
                IsPrivated = waifu.IsPrivated,
                ImageUrl = waifu.ImageUrl,
                AudioId = waifu.AudioId,
            };

            return Ok(OperationResult<WaifuDto?>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating waifu with ShikiId: {ShikiId}", shikiId);
            return Ok(OperationResult<WaifuDto?>.Fail("Ошибка при обновлении вайфу"));
        }
    }

    [HttpDelete("waifus/{shikiId}")]
    public async Task<ActionResult<OperationResult>> DeleteWaifu(
        string shikiId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var waifu = await db.Waifus.FirstOrDefaultAsync(
                w => w.ShikiId == shikiId,
                cancellationToken
            );

            if (waifu == null)
            {
                return Ok(OperationResult.Fail($"Вайфу с ID {shikiId} не найдена"));
            }

            db.Waifus.Remove(waifu);
            await db.SaveChangesAsync(cancellationToken);

            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting waifu with ShikiId: {ShikiId}", shikiId);
            return Ok(OperationResult.Fail("Ошибка при удалении вайфу"));
        }
    }

    #endregion

    #region Husband Endpoints

    [HttpGet("husbands")]
    public async Task<ActionResult<OperationResult<List<HusbandDto>>>> GetAllHusbands(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var husbands = await db
                .Husbands.AsNoTracking()
                .OrderByDescending(h => h.WhenOrdered)
                .Select(h => new HusbandDto
                {
                    TwitchId = h.TwitchId,
                    WhenOrdered = h.WhenOrdered,
                    WaifuBrideId = h.WaifuBrideId,
                    IsPrivated = h.IsPrivated,
                    OrderCount = h.OrderCount,
                    WaifuRollId = h.WaifuRollId,
                    WhenPrivated = h.WhenPrivated,
                    LastWeddingCongratulatedMonths = h.LastWeddingCongratulatedMonths,
                })
                .ToListAsync(cancellationToken);

            return Ok(OperationResult<List<HusbandDto>>.Ok(husbands));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting husbands");
            return Ok(OperationResult<List<HusbandDto>>.Fail("Ошибка при получении мужей"));
        }
    }

    [HttpGet("husbands/{twitchId}")]
    public async Task<ActionResult<OperationResult<HusbandDto?>>> GetHusband(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var husband = await db
                .Husbands.AsNoTracking()
                .Where(h => h.TwitchId == twitchId)
                .Select(h => new HusbandDto
                {
                    TwitchId = h.TwitchId,
                    WhenOrdered = h.WhenOrdered,
                    WaifuBrideId = h.WaifuBrideId,
                    IsPrivated = h.IsPrivated,
                    OrderCount = h.OrderCount,
                    WaifuRollId = h.WaifuRollId,
                    WhenPrivated = h.WhenPrivated,
                    LastWeddingCongratulatedMonths = h.LastWeddingCongratulatedMonths,
                })
                .FirstOrDefaultAsync(cancellationToken);

            return husband != null
                ? Ok(OperationResult<HusbandDto?>.Ok(husband))
                : Ok(OperationResult<HusbandDto?>.Fail($"Муж с ID {twitchId} не найден"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting husband with TwitchId: {TwitchId}", twitchId);
            return Ok(OperationResult<HusbandDto?>.Fail("Ошибка при получении мужа"));
        }
    }

    [HttpPut("husbands/{twitchId}")]
    public async Task<ActionResult<OperationResult<HusbandDto?>>> UpdateHusband(
        string twitchId,
        UpdateHusbandRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (request == null)
            {
                return Ok(OperationResult<HusbandDto?>.Fail("Тело запроса не может быть пустым"));
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var husband = await db.Husbands.FirstOrDefaultAsync(
                h => h.TwitchId == twitchId,
                cancellationToken
            );

            if (husband == null)
            {
                return Ok(OperationResult<HusbandDto?>.Fail($"Муж с ID {twitchId} не найден"));
            }

            if (request.WaifuBrideId != null || request.WaifuBrideId == null)
                husband.WaifuBrideId = request.WaifuBrideId;
            if (request.IsPrivated.HasValue)
                husband.IsPrivated = request.IsPrivated.Value;
            if (request.WaifuRollId != null || request.WaifuRollId == null)
                husband.WaifuRollId = request.WaifuRollId;
            if (request.WhenPrivated.HasValue || request.WhenPrivated == null)
                husband.WhenPrivated = request.WhenPrivated;
            if (request.LastWeddingCongratulatedMonths.HasValue)
                husband.LastWeddingCongratulatedMonths = request.LastWeddingCongratulatedMonths;

            await db.SaveChangesAsync(cancellationToken);

            var dto = new HusbandDto
            {
                TwitchId = husband.TwitchId,
                WhenOrdered = husband.WhenOrdered,
                WaifuBrideId = husband.WaifuBrideId,
                IsPrivated = husband.IsPrivated,
                OrderCount = husband.OrderCount,
                WaifuRollId = husband.WaifuRollId,
                WhenPrivated = husband.WhenPrivated,
                LastWeddingCongratulatedMonths = husband.LastWeddingCongratulatedMonths,
            };

            return Ok(OperationResult<HusbandDto?>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating husband with TwitchId: {TwitchId}", twitchId);
            return Ok(OperationResult<HusbandDto?>.Fail("Ошибка при обновлении мужа"));
        }
    }

    [HttpDelete("husbands/{twitchId}")]
    public async Task<ActionResult<OperationResult>> DeleteHusband(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var husband = await db.Husbands.FirstOrDefaultAsync(
                h => h.TwitchId == twitchId,
                cancellationToken
            );

            if (husband == null)
            {
                return Ok(OperationResult.Fail($"Муж с ID {twitchId} не найден"));
            }

            db.Husbands.Remove(husband);
            await db.SaveChangesAsync(cancellationToken);

            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting husband with TwitchId: {TwitchId}", twitchId);
            return Ok(OperationResult.Fail("Ошибка при удалении мужа"));
        }
    }

    [HttpPost("husbands/{twitchId}/unmerge")]
    public async Task<ActionResult<OperationResult<object?>>> UnmergeHusband(
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
                return Ok(OperationResult<object?>.Fail("Муж не найден или не в браке"));
            }

            var waifu = await db.Waifus.FirstOrDefaultAsync(
                w => w.ShikiId == host.WaifuBrideId,
                cancellationToken
            );

            if (waifu is not { IsPrivated: true })
            {
                return Ok(
                    OperationResult<object?>.Fail(
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

            var data = new
            {
                host.TwitchId,
                waifuId = waifu.ShikiId,
                waifuName = waifu.Name,
            };

            return Ok(OperationResult<object?>.Ok(data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unmerging husband with TwitchId: {TwitchId}", twitchId);
            return Ok(OperationResult<object?>.Fail("Ошибка при разводе"));
        }
    }

    #endregion

    #region Audio Endpoints

    [HttpGet("audios")]
    public async Task<ActionResult<OperationResult<List<WaifuRollAudioDto>>>> GetAllAudios(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var audios = await db
                .WaifuRollAudios.AsNoTracking()
                .OrderBy(a => a.Name)
                .Select(a => new WaifuRollAudioDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    FileExtension = a.FileExtension,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return Ok(OperationResult<List<WaifuRollAudioDto>>.Ok(audios));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting audio tracks");
            return Ok(OperationResult<List<WaifuRollAudioDto>>.Fail("Ошибка при получении аудио"));
        }
    }

    [HttpPost("audios")]
    public async Task<ActionResult<OperationResult<WaifuRollAudioDto?>>> UploadAudio(
        IFormFile file,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return Ok(OperationResult<WaifuRollAudioDto?>.Fail("Файл не может быть пустым"));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(OperationResult<WaifuRollAudioDto?>.Fail("Имя не может быть пустым"));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".mp3" or ".wav" or ".ogg" or ".m4a" or ".flac"))
            {
                return Ok(
                    OperationResult<WaifuRollAudioDto?>.Fail(
                        "Поддерживаемые форматы: mp3, wav, ogg, m4a, flac"
                    )
                );
            }

            byte[] audioData;
            await using (var stream = file.OpenReadStream())
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, cancellationToken);
                audioData = ms.ToArray();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var audio = new WaifuRollAudio
            {
                Name = name,
                AudioData = audioData,
                FileExtension = extension,
            };

            db.WaifuRollAudios.Add(audio);
            await db.SaveChangesAsync(cancellationToken);

            var dto = new WaifuRollAudioDto
            {
                Id = audio.Id,
                Name = audio.Name,
                FileExtension = audio.FileExtension,
                CreatedAt = audio.CreatedAt,
            };

            return Ok(OperationResult<WaifuRollAudioDto?>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading audio");
            return Ok(OperationResult<WaifuRollAudioDto?>.Fail("Ошибка при загрузке аудио"));
        }
    }

    [HttpGet("audios/{id:guid}/stream")]
    public async Task<IActionResult> StreamAudio(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var audio = await db
                .WaifuRollAudios.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (audio == null)
            {
                return NotFound();
            }

            var contentType = audio.FileExtension.ToLowerInvariant() switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".flac" => "audio/flac",
                _ => "application/octet-stream",
            };

            return File(audio.AudioData, contentType, $"{audio.Name}{audio.FileExtension}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error streaming audio with Id: {Id}", id);
            return StatusCode(500);
        }
    }

    [HttpDelete("audios/{id:guid}")]
    public async Task<ActionResult<OperationResult>> DeleteAudio(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var audio = await db.WaifuRollAudios.FirstOrDefaultAsync(
                a => a.Id == id,
                cancellationToken
            );

            if (audio == null)
            {
                return Ok(OperationResult.Fail("Аудио не найдено"));
            }

            db.WaifuRollAudios.Remove(audio);
            await db.SaveChangesAsync(cancellationToken);

            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting audio with Id: {Id}", id);
            return Ok(OperationResult.Fail("Ошибка при удалении аудио"));
        }
    }

    #endregion
}
