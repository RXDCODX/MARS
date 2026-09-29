using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class MikuRollService(IDbContextFactory<WaifuDbContext> factory)
{
    public async Task<MikuModule?> RollTheMiku()
    {
        MikuModule? result = null;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var module = await dbContext
                .MikuModules.OrderBy(e => e.LastOrder)
                .FirstOrDefaultAsync();

            if (module != null)
            {
                module.OrderCount++;
                module.LastOrder = DateTime.Now;

                dbContext.MikuModules.Update(module);
                await dbContext.SaveChangesAsync();

                result = module;
            }
        }
        catch
        {
            // Ошибка при ролле - возвращаем null
        }

        return result;
    }

    public async Task<OperationResult<ICollection<MikuPrizeType>>> GetMikuPrizesAsync()
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var prizes = new List<MikuPrizeType>();

            var modules = await dbContext
                .MikuModules.AsNoTracking()
                .OrderBy(e => e.PageId)
                .ToListAsync();

            foreach (var module in modules)
            {
                prizes.Add(
                    new MikuPrizeType
                    {
                        Id = module.PageId.ToString(),
                        Image = module.ThumbnailUrl,
                        Text = module.JapaneseName ?? module.Title,
                    }
                );
            }

            return OperationResult<ICollection<MikuPrizeType>>.Ok(prizes);
        }
        catch (Exception ex)
        {
            return OperationResult<ICollection<MikuPrizeType>>.Fail(
                $"Ошибка при получении призов MikuModule: {ex.Message}"
            );
        }
    }
}
