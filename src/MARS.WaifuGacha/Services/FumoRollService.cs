using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class FumoRollService(IDbContextFactory<WaifuDbContext> factory)
{
    public async Task<Fumo?> RollTheFumo()
    {
        Fumo? result = null;

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();

            var fumo = await dbContext.Fumos.OrderBy(e => e.LastOrder).FirstOrDefaultAsync();

            if (fumo != null)
            {
                fumo.OrderCount++;
                fumo.LastOrder = DateTime.Now;

                if (string.IsNullOrWhiteSpace(fumo.CharacterTranslit))
                {
                    fumo.CharacterTranslit = TransliterateToRussian(fumo.Character);
                }

                dbContext.Fumos.Update(fumo);
                await dbContext.SaveChangesAsync();

                result = fumo;
            }
        }
        catch
        {
            // Ошибка при ролле - возвращаем null
        }

        return result;
    }

    public async Task<OperationResult<ICollection<FumoPrizeType>>> GetFumoPrizesAsync()
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var prizes = new List<FumoPrizeType>();

            var fumos = await dbContext.Fumos.AsNoTracking().OrderBy(e => e.MfcId).ToListAsync();

            foreach (var fumo in fumos)
            {
                if (string.IsNullOrWhiteSpace(fumo.CharacterTranslit))
                {
                    fumo.CharacterTranslit = TransliterateToRussian(fumo.Character);
                }

                prizes.Add(
                    new FumoPrizeType
                    {
                        Id = fumo.MfcId.ToString(),
                        Image = fumo.ThumbnailUrl,
                        Text = fumo.CharacterTranslit ?? fumo.Name,
                    }
                );
            }

            return OperationResult<ICollection<FumoPrizeType>>.Ok(prizes);
        }
        catch (Exception ex)
        {
            return OperationResult<ICollection<FumoPrizeType>>.Fail(
                $"Ошибка при получении призов Fumo: {ex.Message}"
            );
        }
    }

    private static string TransliterateToRussian(string input)
    {
        var map = new Dictionary<char, string>
        {
            { 'a', "а" },
            { 'b', "б" },
            { 'v', "в" },
            { 'g', "г" },
            { 'd', "д" },
            { 'e', "е" },
            { 'ё', "ё" },
            { 'z', "з" },
            { 'i', "и" },
            { 'y', "й" },
            { 'k', "к" },
            { 'l', "л" },
            { 'm', "м" },
            { 'n', "н" },
            { 'o', "о" },
            { 'p', "п" },
            { 'r', "р" },
            { 's', "с" },
            { 't', "т" },
            { 'u', "у" },
            { 'f', "ф" },
            { 'h', "х" },
            { 'c', "ц" },
            { 'j', "ж" },
            { 'x', "кс" },
            { 'w', "в" },
            { 'q', "к" },
        };

        var result = new System.Text.StringBuilder(input.Length * 2);
        foreach (var c in input.ToLowerInvariant())
        {
            result.Append(map.TryGetValue(c, out var ru) ? ru : c.ToString());
        }

        return result.ToString();
    }
}
