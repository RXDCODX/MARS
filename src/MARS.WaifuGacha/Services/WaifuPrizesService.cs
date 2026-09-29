using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Services;

public class WaifuPrizesService(
    IDbContextFactory<WaifuDbContext> factory,
    IOptions<ShikimoriClientOptions> shikiOptions,
    ILogger<WaifuPrizesService> logger
)
{
    private string ShikimoriSite =>
        shikiOptions.Value.ShikimoriSite.EndsWith('/')
            ? shikiOptions.Value.ShikimoriSite[..^1]
            : shikiOptions.Value.ShikimoriSite;

    public async Task<OperationResult<ICollection<PrizeType>>> GetWaifuPrizesAsync()
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var prizes = new List<PrizeType>();

            const int batchSize = 50;
            var offset = 0;

            while (true)
            {
                var waifusBatch = await dbContext
                    .Waifus.AsNoTracking()
                    .OrderBy(e => e.ShikiId)
                    .Skip(offset)
                    .Take(batchSize)
                    .ToListAsync();

                if (waifusBatch.Count == 0)
                {
                    break;
                }

                foreach (var waifu in waifusBatch)
                {
                    var imageUrl = waifu.ImageUrl.StartsWith('/')
                        ? waifu.ImageUrl
                        : "/" + waifu.ImageUrl;

                    prizes.Add(
                        new PrizeType
                        {
                            Id = waifu.ShikiId,
                            Image = ShikimoriSite + imageUrl,
                            Text = waifu.Name,
                        }
                    );
                }

                offset += batchSize;
            }

            return OperationResult<ICollection<PrizeType>>.Ok(prizes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting waifu prizes");
            return OperationResult<ICollection<PrizeType>>.Fail(
                $"Ошибка при получении призов вайфу: {ex.Message}"
            );
        }
    }
}
