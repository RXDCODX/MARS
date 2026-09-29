using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Models;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class WaifuRollGuaranteeService(
    IDbContextFactory<WaifuDbContext> dbContextFactory,
    ILogger<WaifuRollGuaranteeService> logger
)
{
    private const int GuaranteeRolls = 200;
    private const double VipChance = 0.015;

    public async Task<OperationResult<VipDropResponse>> CheckVipDropAsync(string twitchId)
    {
        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return OperationResult<VipDropResponse>.Fail("TwitchId не может быть пустым");
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var guarantee = await dbContext
                .WaifuRollGuarantees.AsNoTracking()
                .FirstOrDefaultAsync(g => g.TwitchId == twitchId);

            var vipResponse = new VipDropResponse { RollCount = guarantee?.RollCount ?? 0 };

            if (guarantee is { RollCount: >= GuaranteeRolls })
            {
                vipResponse.IsVipDropped = true;
                vipResponse.DropReason = "Гарант";
                await DeleteGuaranteeAsync(twitchId);

                return OperationResult<VipDropResponse>.Ok(vipResponse);
            }

            var random = Random.Shared.NextDouble();
            vipResponse.IsVipDropped = random <= VipChance;

            if (vipResponse.IsVipDropped)
            {
                vipResponse.DropReason = "Случайность";
                await ResetRollCountAsync(twitchId);
            }
            else
            {
                vipResponse.DropReason = "Не выпал";
            }

            return OperationResult<VipDropResponse>.Ok(vipResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking VIP drop for {TwitchId}", twitchId);
            return OperationResult<VipDropResponse>.Fail(
                $"Ошибка при проверке VIP статуса: {ex.Message}"
            );
        }
    }

    public async Task<OperationResult<RollCountResponse>> IncrementRollCountAsync(string twitchId)
    {
        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return OperationResult<RollCountResponse>.Fail("TwitchId не может быть пустым");
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var guarantee = await dbContext.WaifuRollGuarantees.FirstOrDefaultAsync(g =>
                g.TwitchId == twitchId
            );

            if (guarantee == null)
            {
                guarantee = new WaifuRollGuarantee
                {
                    TwitchId = twitchId,
                    RollCount = 1,
                    LastRoll = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                };
                await dbContext.WaifuRollGuarantees.AddAsync(guarantee);
            }
            else
            {
                guarantee.RollCount++;
                guarantee.LastRoll = DateTime.Now;
                guarantee.UpdatedAt = DateTime.Now;
            }

            await dbContext.SaveChangesAsync();

            return OperationResult<RollCountResponse>.Ok(
                new RollCountResponse
                {
                    Success = true,
                    CurrentRollCount = guarantee.RollCount,
                    Message = "Счетчик роллов успешно увеличен",
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing roll count for {TwitchId}", twitchId);
            return OperationResult<RollCountResponse>.Fail(
                $"Ошибка при увеличении счетчика: {ex.Message}"
            );
        }
    }

    public async Task<OperationResult<WaifuRollGuarantee?>> GetGuaranteeInfoAsync(string twitchId)
    {
        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return OperationResult<WaifuRollGuarantee?>.Fail("TwitchId не может быть пустым");
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var guarantee = await dbContext
                .WaifuRollGuarantees.AsNoTracking()
                .FirstOrDefaultAsync(g => g.TwitchId == twitchId);

            return OperationResult<WaifuRollGuarantee?>.Ok(guarantee);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting guarantee info for {TwitchId}", twitchId);
            return OperationResult<WaifuRollGuarantee?>.Fail(
                $"Ошибка при получении информации: {ex.Message}"
            );
        }
    }

    public async Task<OperationResult<RollCountResponse>> ResetRollCountAsync(string twitchId)
    {
        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return OperationResult<RollCountResponse>.Fail("TwitchId не может быть пустым");
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var guarantee = await dbContext.WaifuRollGuarantees.FirstOrDefaultAsync(g =>
                g.TwitchId == twitchId
            );

            if (guarantee == null)
            {
                return OperationResult<RollCountResponse>.Fail(
                    "Пользователь не найден в системе гарантов"
                );
            }

            guarantee.RollCount = 0;
            guarantee.UpdatedAt = DateTime.Now;
            await dbContext.SaveChangesAsync();

            return OperationResult<RollCountResponse>.Ok(
                new RollCountResponse
                {
                    Success = true,
                    CurrentRollCount = 0,
                    Message = "Счетчик роллов успешно сброшен",
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting roll count for {TwitchId}", twitchId);
            return OperationResult<RollCountResponse>.Fail(
                $"Ошибка при сбросе счетчика: {ex.Message}"
            );
        }
    }

    public async Task<OperationResult<bool>> DeleteGuaranteeAsync(string twitchId)
    {
        if (string.IsNullOrWhiteSpace(twitchId))
        {
            return OperationResult<bool>.Fail("TwitchId не может быть пустым");
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var guarantee = await dbContext.WaifuRollGuarantees.FirstOrDefaultAsync(g =>
                g.TwitchId == twitchId
            );

            if (guarantee == null)
            {
                return OperationResult<bool>.Fail("Пользователь не найден в системе гарантов");
            }

            dbContext.WaifuRollGuarantees.Remove(guarantee);
            await dbContext.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting guarantee for {TwitchId}", twitchId);
            return OperationResult<bool>.Fail($"Ошибка при удалении: {ex.Message}");
        }
    }
}
