using MARS.Shared.Clients;
using MARS.Shikimori.Data;
using MARS.Shikimori.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Shikimori.Services;

/// <summary>
/// Хранение выбранного на Shikimori: персонажи и история выдачи произведений.
/// </summary>
/// <remarks>
/// Пункт W3 — «собственная БД хранит информацию по аниме, манге и персонажам».
/// Данные всегда берутся у Shikimori свежими (так же, как в монолите), а сюда
/// попадает то, что уже показали зрителям: по таблице видно, какие персонажи
/// добавлялись в вайфу и какие произведения выпадали командам.
/// </remarks>
public class ShikimoriCatalog(
    IDbContextFactory<ShikimoriDbContext> dbContextFactory,
    ILogger<ShikimoriCatalog> logger
)
{
    /// <summary>
    /// Сохраняет персонажа и возвращает его же из базы — тем же контрактом,
    /// что ушёл наружу.
    /// </summary>
    public async Task<ShikimoriCharacterRef> SaveCharacterAsync(
        ShikimoriCharacterRef character,
        CancellationToken cancellationToken = default
    )
    {
        var result = character;

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var stored = await dbContext.Characters.FirstOrDefaultAsync(
                entity => entity.Id == character.Id,
                cancellationToken
            );

            if (stored is null)
            {
                stored = new ShikimoriCharacter { Id = character.Id };
                await dbContext.Characters.AddAsync(stored, cancellationToken);
            }

            stored.Name = character.Name;
            stored.RussianName = character.RussianName;
            stored.Description = character.Description;
            stored.ImageUrl = character.ImageUrl;
            stored.ImagePath = character.ImagePath;
            stored.AnimeTitle = character.AnimeTitle;
            stored.MangaTitle = character.MangaTitle;
            stored.SyncedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            result = ToRef(stored);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Ответ зрителя не должен зависеть от того, записалась ли история:
            // Shikimori уже ответил, и данные у потребителя есть.
            logger.LogWarning(
                ex,
                "Не удалось сохранить персонажа {CharacterId} в базе",
                character.Id
            );
        }

        return result;
    }

    /// <summary>Записывает выданное произведение в историю.</summary>
    public async Task RecordPickAsync(
        string kind,
        ShikimoriTitleRef title,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            await dbContext.TitlePicks.AddAsync(
                new ShikimoriTitlePick
                {
                    Kind = kind,
                    ShikimoriId = title.Id,
                    Name = title.Name,
                    RussianName = title.RussianName,
                    Year = title.Year,
                    Url = title.Url,
                    PickedAtUtc = DateTime.UtcNow,
                },
                cancellationToken
            );

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось записать выдачу {Kind} #{Id}", kind, title.Id);
        }
    }

    /// <summary>
    /// Персонаж из базы или null, если его там ещё не было. Нужен тем, кто
    /// хочет посмотреть историю без обращения к Shikimori.
    /// </summary>
    public async Task<ShikimoriCharacterRef?> FindCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        var result = default(ShikimoriCharacterRef?);

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var stored = await dbContext
                .Characters.AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

            if (stored is not null)
            {
                result = ToRef(stored);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось прочитать персонажа {CharacterId}", id);
        }

        return result;
    }

    private static ShikimoriCharacterRef ToRef(ShikimoriCharacter stored)
    {
        return new ShikimoriCharacterRef(
            stored.Id,
            stored.Name,
            stored.RussianName,
            stored.Description,
            stored.ImageUrl,
            stored.ImagePath,
            stored.AnimeTitle,
            stored.MangaTitle
        );
    }
}
