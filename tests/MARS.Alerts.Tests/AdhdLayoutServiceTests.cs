using MARS.Alerts.Data;
using MARS.Alerts.Entities;
using MARS.Alerts.Models;
using MARS.Alerts.Services.Adhd;
using Microsoft.EntityFrameworkCore;

namespace MARS.Alerts.Tests;

/// <summary>
/// Чтение и запись настройки раскладки ADHD-экрана.
/// Провайдер — InMemory: проверяется поведение сервиса, а не SQL.
/// </summary>
public class AdhdLayoutServiceTests
{
    private sealed class TestDbContextFactory(string databaseName)
        : IDbContextFactory<AlertsDbContext>
    {
        public AlertsDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AlertsDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new AlertsDbContext(options);
        }
    }

    private static (AdhdLayoutService Service, IDbContextFactory<AlertsDbContext> Factory) Build(
        string databaseName
    )
    {
        var factory = new TestDbContextFactory(databaseName);

        return (new AdhdLayoutService(factory), factory);
    }

    private static async Task SeedAsync(
        IDbContextFactory<AlertsDbContext> factory,
        Action<AdhdLayoutConfig> customize
    )
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = factory.CreateDbContext();

        var row = new AdhdLayoutConfig
        {
            Id = 1,
            CreatedAt = new DateTime(2026, 9, 7, 19, 36, 0, DateTimeKind.Utc),
        };
        customize(row);
        context.AdhdLayoutConfig.Add(row);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Пока строки нет, оверлей должен показать весь набор виджетов: в монолите
    /// пустая таблица давала конфигурацию по умолчанию, а не «всё выключено».
    /// </summary>
    [Fact]
    public async Task GetAsync_ReturnsFullLayout_WhenTableIsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(nameof(GetAsync_ReturnsFullLayout_WhenTableIsEmpty));

        var result = await service.GetAsync(ct);

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.True(result.Result!.ShowRainEffect);
        Assert.True(result.Result.ShowTimer);
        Assert.True(result.Result.ShowLOFIGirl);
        Assert.Equal(12, result.Result.DvdLogosCount);
    }

    [Fact]
    public async Task GetAsync_ReturnsStoredConfig_WhenRowExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, factory) = Build(nameof(GetAsync_ReturnsStoredConfig_WhenRowExists));

        await SeedAsync(
            factory,
            row =>
            {
                row.ShowRainEffect = false;
                row.DvdLogosCount = 3;
            }
        );

        var result = await service.GetAsync(ct);

        Assert.True(result.Success);
        Assert.False(result.Result!.ShowRainEffect);
        Assert.Equal(3, result.Result.DvdLogosCount);
    }

    /// <summary>
    /// Настройка одна на весь сервис, поэтому первая запись создаётся с
    /// фиксированным ключом: автоинкрементом получилась бы вторая строка,
    /// конфликтующая по первичному ключу.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CreatesTheOnlyRow_WhenTableIsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, factory) = Build(nameof(UpdateAsync_CreatesTheOnlyRow_WhenTableIsEmpty));

        var result = await service.UpdateAsync(new AdhdLayoutConfigDto { DvdLogosCount = 7 }, ct);

        Assert.True(result.Success);

        await using var context = factory.CreateDbContext();
        var stored = await context.AdhdLayoutConfig.SingleAsync(ct);

        Assert.Equal(1, stored.Id);
        Assert.Equal(7, stored.DvdLogosCount);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ReusesTheExistingRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, factory) = Build(nameof(UpdateAsync_ReusesTheExistingRow));

        await SeedAsync(factory, row => row.DvdLogosCount = 3);

        var result = await service.UpdateAsync(new AdhdLayoutConfigDto { DvdLogosCount = 9 }, ct);

        Assert.True(result.Success);

        await using var context = factory.CreateDbContext();

        Assert.Equal(1, await context.AdhdLayoutConfig.CountAsync(ct));
        Assert.Equal(9, (await context.AdhdLayoutConfig.SingleAsync(ct)).DvdLogosCount);
    }

    /// <summary>
    /// Все 15 переключателей и счётчик логотипов должны переживать цикл
    /// записи: потеря одного из них означала бы молчаливую смену настроек.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_PersistsEveryWidgetFlag()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(nameof(UpdateAsync_PersistsEveryWidgetFlag));

        var written = new AdhdLayoutConfigDto
        {
            ShowRainEffect = false,
            ShowDVDLogos = false,
            ShowBreakingNews = false,
            ShowStreamerVideo = false,
            ShowFitnessVideo = false,
            ShowGTAVideo = false,
            ShowHydraulicMobileVideo = false,
            ShowSlimeVideo = false,
            ShowMukbangVideo = false,
            ShowQuiz = false,
            ShowSurfer = false,
            ShowLOFIGirl = false,
            ShowCatisa = false,
            ShowNotifications = false,
            ShowTimer = false,
            DvdLogosCount = 1,
        };

        await service.UpdateAsync(written, ct);
        var read = await service.GetAsync(ct);

        Assert.False(read.Result!.ShowRainEffect);
        Assert.False(read.Result.ShowDVDLogos);
        Assert.False(read.Result.ShowBreakingNews);
        Assert.False(read.Result.ShowStreamerVideo);
        Assert.False(read.Result.ShowFitnessVideo);
        Assert.False(read.Result.ShowGTAVideo);
        Assert.False(read.Result.ShowHydraulicMobileVideo);
        Assert.False(read.Result.ShowSlimeVideo);
        Assert.False(read.Result.ShowMukbangVideo);
        Assert.False(read.Result.ShowQuiz);
        Assert.False(read.Result.ShowSurfer);
        Assert.False(read.Result.ShowLOFIGirl);
        Assert.False(read.Result.ShowCatisa);
        Assert.False(read.Result.ShowNotifications);
        Assert.False(read.Result.ShowTimer);
        Assert.Equal(1, read.Result.DvdLogosCount);
    }
}
