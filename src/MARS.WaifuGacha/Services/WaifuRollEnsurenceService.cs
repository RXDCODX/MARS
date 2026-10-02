using MARS.Shared.Clients;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class WaifuRollEnsurenceService(
    ILogger<WaifuRollEnsurenceService> logger,
    IShikimoriApiClient shikimoriClient,
    IDbContextFactory<WaifuDbContext> dbContextFactory
)
{
    public async Task<Waifu> EnsureWaifuHaveImageIrl(Waifu waifu)
    {
        if (string.IsNullOrWhiteSpace(waifu.ImageUrl))
        {
            var character = await shikimoriClient.GetCharacterAsync(long.Parse(waifu.ShikiId));
            if (character is not null)
            {
                waifu.ImageUrl = character.ImageUrl;
            }
        }

        return waifu;
    }

    public async Task<Waifu> EnsureMangaAndAnimeTitleExists(
        Waifu waifu,
        WaifuDbContext? dbContext = null
    )
    {
        var result = waifu;
        var isContextNull = dbContext == null;

        try
        {
            if (
                string.IsNullOrWhiteSpace(waifu.Manga)
                && string.IsNullOrWhiteSpace(waifu.Anime)
                && long.TryParse(waifu.ShikiId, out var characterId)
            )
            {
                dbContext ??= await dbContextFactory.CreateDbContextAsync();

                // Один запрос на оба названия: раньше аниме и манга тянулись
                // двумя обращениями к Shikimori через один и тот же персонаж.
                var character = await shikimoriClient.GetCharacterAsync(characterId);

                if (string.IsNullOrWhiteSpace(result.Anime))
                {
                    var animeTitle = character?.AnimeTitle;

                    if (!string.IsNullOrWhiteSpace(animeTitle))
                    {
                        result.Anime = animeTitle;
                        dbContext.Waifus.Update(result);
                    }
                }

                if (string.IsNullOrWhiteSpace(result.Manga))
                {
                    var mangaTitle = character?.MangaTitle;
                    if (!string.IsNullOrWhiteSpace(mangaTitle))
                    {
                        result.Manga = mangaTitle;
                        dbContext.Waifus.Update(result);
                    }
                }

                if (!isContextNull)
                {
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при заполнении полей аниме и манги для вайфу {WaifuId}",
                waifu.ShikiId
            );
        }

        return result;
    }
}
