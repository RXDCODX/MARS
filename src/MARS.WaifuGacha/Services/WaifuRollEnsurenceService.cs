using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class WaifuRollEnsurenceService(
    ILogger<WaifuRollEnsurenceService> logger,
    ShikimoriService shikiService,
    IDbContextFactory<WaifuDbContext> dbContextFactory
)
{
    public async Task<Waifu> EnsureWaifuHaveImageIrl(Waifu waifu)
    {
        if (string.IsNullOrWhiteSpace(waifu.ImageUrl))
        {
            var character = await shikiService.GetShikiCharacterById(long.Parse(waifu.ShikiId));
            if (character != null)
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync();
                waifu.ImageUrl = character.Image.Original;
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
                if (string.IsNullOrWhiteSpace(result.Anime))
                {
                    var animeTitle = await shikiService.GetCharacterAnimeTitle(characterId);
                    if (!string.IsNullOrWhiteSpace(animeTitle))
                    {
                        result.Anime = animeTitle;
                        dbContext.Waifus.Update(result);
                    }
                }

                if (string.IsNullOrWhiteSpace(result.Manga))
                {
                    var mangaTitle = await shikiService.GetCharacterMangaTitle(characterId);
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
