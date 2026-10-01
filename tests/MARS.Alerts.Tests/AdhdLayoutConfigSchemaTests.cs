using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MARS.Alerts.Data;
using MARS.Alerts.Entities;

namespace MARS.Alerts.Tests;

/// <summary>
/// Проверки схемы mars_alerts. Сервис стал владельцем базы только ради
/// AdhdLayoutConfig: до этого он был stateless, и хранить ему было нечего.
/// </summary>
public class AdhdLayoutConfigSchemaTests
{
    private static AlertsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AlertsDbContext>()
            .UseInMemoryDatabase("test")
            .Options;

        return new AlertsDbContext(options);
    }

    [Fact]
    public void AdhdLayoutConfig_LivesInAlertsSchema()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(AdhdLayoutConfig));

        Assert.Equal("alerts", entity!.GetSchema());
        Assert.Equal("AdhdLayoutConfig", entity.GetTableName());
    }

    /// <summary>
    /// Ключ задаётся явно и не генерируется: настройка одна на весь сервис, и
    /// автоинкремент привёл бы ко второй строке с Id = 1, конфликтующей по PK.
    /// </summary>
    [Fact]
    public void AdhdLayoutConfig_IdIsNotGenerated()
    {
        using var context = CreateContext();

        var property = context
            .Model.FindEntityType(typeof(AdhdLayoutConfig))!
            .FindProperty(nameof(AdhdLayoutConfig.Id));

        Assert.Equal(ValueGenerated.Never, property!.ValueGenerated);
    }

    /// <summary>
    /// UpdatedAt в монолите был nullable: первоначальная запись создавалась
    /// одним INSERT без последующего UPDATE.
    /// </summary>
    [Fact]
    public void AdhdLayoutConfig_UpdatedAtIsNullable()
    {
        using var context = CreateContext();

        var property = context
            .Model.FindEntityType(typeof(AdhdLayoutConfig))!
            .FindProperty(nameof(AdhdLayoutConfig.UpdatedAt));

        Assert.True(property!.IsNullable);
    }

    /// <summary>
    /// Все 15 переключателей и DvdLogosCount перенесены из монолита: потеря
    /// одного из них означала бы молчаливую смену настроек у пользователя.
    /// </summary>
    [Fact]
    public void AdhdLayoutConfig_KeepsAllLegacyColumns()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(AdhdLayoutConfig))!;
        var expected = new[]
        {
            nameof(AdhdLayoutConfig.Id),
            nameof(AdhdLayoutConfig.ShowRainEffect),
            nameof(AdhdLayoutConfig.ShowDVDLogos),
            nameof(AdhdLayoutConfig.ShowBreakingNews),
            nameof(AdhdLayoutConfig.ShowStreamerVideo),
            nameof(AdhdLayoutConfig.ShowFitnessVideo),
            nameof(AdhdLayoutConfig.ShowGTAVideo),
            nameof(AdhdLayoutConfig.ShowHydraulicMobileVideo),
            nameof(AdhdLayoutConfig.ShowSlimeVideo),
            nameof(AdhdLayoutConfig.ShowMukbangVideo),
            nameof(AdhdLayoutConfig.ShowQuiz),
            nameof(AdhdLayoutConfig.ShowSurfer),
            nameof(AdhdLayoutConfig.ShowLOFIGirl),
            nameof(AdhdLayoutConfig.ShowCatisa),
            nameof(AdhdLayoutConfig.ShowNotifications),
            nameof(AdhdLayoutConfig.CreatedAt),
            nameof(AdhdLayoutConfig.UpdatedAt),
            nameof(AdhdLayoutConfig.DvdLogosCount),
            nameof(AdhdLayoutConfig.ShowTimer),
        };

        var actual = entity.GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();
        var missing = expected.Except(actual).ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public async Task AdhdLayoutConfig_PersistsManualTuning()
    {
        await using var context = new AlertsDbContext(
            new DbContextOptionsBuilder<AlertsDbContext>()
                .UseInMemoryDatabase(nameof(AdhdLayoutConfig_PersistsManualTuning))
                .Options
        );

        context.AdhdLayoutConfig.Add(
            new AdhdLayoutConfig
            {
                Id = 1,
                ShowRainEffect = false,
                ShowBreakingNews = true,
                DvdLogosCount = 5,
                CreatedAt = new DateTime(2026, 9, 7, 19, 36, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 9, 7, 21, 10, 0, DateTimeKind.Utc),
            }
        );
        await context.SaveChangesAsync();

        await using var verify = new AlertsDbContext(
            new DbContextOptionsBuilder<AlertsDbContext>()
                .UseInMemoryDatabase(nameof(AdhdLayoutConfig_PersistsManualTuning))
                .Options
        );
        var stored = await verify.AdhdLayoutConfig.SingleAsync();

        Assert.Equal(1, stored.Id);
        Assert.False(stored.ShowRainEffect);
        Assert.True(stored.ShowBreakingNews);
        Assert.Equal(5, stored.DvdLogosCount);
        Assert.NotNull(stored.UpdatedAt);
    }
}
