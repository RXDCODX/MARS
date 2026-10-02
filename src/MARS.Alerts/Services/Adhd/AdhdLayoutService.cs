using MARS.Alerts.Data;
using MARS.Alerts.Entities;
using MARS.Alerts.Models;
using MARS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MARS.Alerts.Services.Adhd;

/// <summary>
/// Настройка раскладки ADHD-экрана: единственная строка, задающая состав
/// декоративных виджетов.
/// </summary>
/// <remarks>
/// Пустая таблица — не ошибка, а «показать всё»: оверлей сразу после старта
/// получает полный набор виджетов и не мигает пустым экраном. Отличие от
/// монолита — результат возвращается, а не бросается, потому что вызывающая
/// сторона (gRPC-контракт TelegramusService) отдаёт ошибку клиенту кодом статуса.
/// </remarks>
public sealed class AdhdLayoutService(IDbContextFactory<AlertsDbContext> factory)
    : IAdhdLayoutService
{
    /// <summary>Ключ единственной строки настройки.</summary>
    private const int SingletonConfigId = 1;

    public async Task<OperationResult<AdhdLayoutConfigDto>> GetAsync(
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<AdhdLayoutConfigDto>.Fail("Стартовая ошибка чтения настройки");

        try
        {
            await using var context = await factory.CreateDbContextAsync(cancellationToken);

            var stored = await context
                .AdhdLayoutConfig.AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            result = stored is null
                ? OperationResult<AdhdLayoutConfigDto>.Ok(new AdhdLayoutConfigDto())
                : OperationResult<AdhdLayoutConfigDto>.Ok(ToDto(stored));
        }
        catch (Exception ex)
        {
            result = OperationResult<AdhdLayoutConfigDto>.Fail(
                $"Не удалось прочитать настройку: {ex.Message}"
            );
        }

        return result;
    }

    public async Task<OperationResult<AdhdLayoutConfigDto>> UpdateAsync(
        AdhdLayoutConfigDto config,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<AdhdLayoutConfigDto>.Fail("Стартовая ошибка записи настройки");

        if (config is not null)
        {
            try
            {
                await using var context = await factory.CreateDbContextAsync(cancellationToken);

                var stored = await context.AdhdLayoutConfig.SingleOrDefaultAsync(cancellationToken);

                if (stored is null)
                {
                    stored = new AdhdLayoutConfig
                    {
                        Id = SingletonConfigId,
                        CreatedAt = DateTime.UtcNow,
                    };
                    context.AdhdLayoutConfig.Add(stored);
                }

                Apply(config, stored);
                stored.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync(cancellationToken);

                result = OperationResult<AdhdLayoutConfigDto>.Ok(config);
            }
            catch (Exception ex)
            {
                result = OperationResult<AdhdLayoutConfigDto>.Fail(
                    $"Не удалось сохранить настройку: {ex.Message}"
                );
            }
        }
        else
        {
            result = OperationResult<AdhdLayoutConfigDto>.Fail("Настройка не задана");
        }

        return result;
    }

    private static AdhdLayoutConfigDto ToDto(AdhdLayoutConfig entity)
    {
        var result = new AdhdLayoutConfigDto
        {
            ShowRainEffect = entity.ShowRainEffect,
            ShowDVDLogos = entity.ShowDVDLogos,
            ShowBreakingNews = entity.ShowBreakingNews,
            ShowStreamerVideo = entity.ShowStreamerVideo,
            ShowFitnessVideo = entity.ShowFitnessVideo,
            ShowGTAVideo = entity.ShowGTAVideo,
            ShowHydraulicMobileVideo = entity.ShowHydraulicMobileVideo,
            ShowSlimeVideo = entity.ShowSlimeVideo,
            ShowMukbangVideo = entity.ShowMukbangVideo,
            ShowQuiz = entity.ShowQuiz,
            ShowSurfer = entity.ShowSurfer,
            ShowLOFIGirl = entity.ShowLOFIGirl,
            ShowCatisa = entity.ShowCatisa,
            ShowNotifications = entity.ShowNotifications,
            ShowTimer = entity.ShowTimer,
            DvdLogosCount = entity.DvdLogosCount,
        };

        return result;
    }

    private static void Apply(AdhdLayoutConfigDto dto, AdhdLayoutConfig entity)
    {
        entity.ShowRainEffect = dto.ShowRainEffect;
        entity.ShowDVDLogos = dto.ShowDVDLogos;
        entity.ShowBreakingNews = dto.ShowBreakingNews;
        entity.ShowStreamerVideo = dto.ShowStreamerVideo;
        entity.ShowFitnessVideo = dto.ShowFitnessVideo;
        entity.ShowGTAVideo = dto.ShowGTAVideo;
        entity.ShowHydraulicMobileVideo = dto.ShowHydraulicMobileVideo;
        entity.ShowSlimeVideo = dto.ShowSlimeVideo;
        entity.ShowMukbangVideo = dto.ShowMukbangVideo;
        entity.ShowQuiz = dto.ShowQuiz;
        entity.ShowSurfer = dto.ShowSurfer;
        entity.ShowLOFIGirl = dto.ShowLOFIGirl;
        entity.ShowCatisa = dto.ShowCatisa;
        entity.ShowNotifications = dto.ShowNotifications;
        entity.ShowTimer = dto.ShowTimer;
        entity.DvdLogosCount = dto.DvdLogosCount;
    }
}
