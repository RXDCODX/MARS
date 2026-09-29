using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class FrogRollService(IDbContextFactory<WaifuDbContext> factory)
{
    public async Task<Frog?> RollTheFrog()
    {
        Frog? result = null;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var frog = await dbContext.Frogs.OrderBy(e => e.LastOrder).FirstOrDefaultAsync();

            if (frog != null)
            {
                frog.OrderCount++;
                frog.LastOrder = DateTime.Now;

                dbContext.Frogs.Update(frog);
                await dbContext.SaveChangesAsync();

                result = frog;
            }
        }
        catch
        {
            // Ошибка при ролле - возвращаем null
        }

        return result;
    }

    public async Task<OperationResult<ICollection<FrogPrizeType>>> GetFrogPrizesAsync()
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var prizes = new List<FrogPrizeType>();

            var frogs = await dbContext.Frogs.AsNoTracking().OrderBy(e => e.Pid).ToListAsync();

            foreach (var frog in frogs)
            {
                prizes.Add(
                    new FrogPrizeType
                    {
                        Id = frog.Pid.ToString(),
                        Image = frog.ThumbnailUrl,
                        Text = frog.RussianName ?? frog.CommonName,
                    }
                );
            }

            return OperationResult<ICollection<FrogPrizeType>>.Ok(prizes);
        }
        catch (Exception ex)
        {
            return OperationResult<ICollection<FrogPrizeType>>.Fail(
                $"Ошибка при получении призов Frog: {ex.Message}"
            );
        }
    }
}
