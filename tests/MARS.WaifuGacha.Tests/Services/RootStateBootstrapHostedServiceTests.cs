using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Создание ключей состояния при старте.
///
/// Ключи кулдаунов читаются из <c>waifu.RootState</c>. Если их нет, конфигурация
/// молча мертва: команда работает со значениями по умолчанию, и администратор не
/// понимает, почему кулдаун не применяется.
/// </summary>
public class RootStateBootstrapHostedServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();

    [Fact]
    public async Task MissingKeysAreCreated()
    {
        var service = Create();

        await service.StartAsync(Token);

        await using var db = await _factory.CreateDbContextAsync(Token);
        foreach (var (key, _) in RootStateKeys.Defaults)
        {
            Assert.True(db.RootState.Any(state => state.Name == key), $"ключ {key} не создан");
        }
    }

    /// <summary>
    /// Повторный запуск не перетирает настроенные значения: администратор менял
    /// кулдаун, а сервис после перезапуска вернул бы значение по умолчанию.
    /// </summary>
    [Fact]
    public async Task ExistingValuesAreKept()
    {
        var service = Create();
        await service.StartAsync(Token);

        var key = RootStateKeys.Defaults.Keys.First();
        await using (var db = await _factory.CreateDbContextAsync(Token))
        {
            var state = db.RootState.Single(entry => entry.Name == key);
            state.Value = "999";
            await db.SaveChangesAsync(Token);
        }

        await service.StartAsync(Token);

        await using var check = await _factory.CreateDbContextAsync(Token);
        Assert.Equal("999", check.RootState.Single(state => state.Name == key).Value);
    }

    /// <summary>
    /// Ошибка базы не роняет запуск сервиса: без ключей сервис работает на
    /// значениях по умолчанию, но продолжает подниматься.
    /// </summary>
    [Fact]
    public async Task DatabaseFailureDoesNotStopStartup()
    {
        var service = new RootStateBootstrapHostedService(
            new FailingFactory(),
            NullLogger<RootStateBootstrapHostedService>.Instance
        );

        await service.StartAsync(Token);
    }

    [Fact]
    public async Task StopDoesNothing()
    {
        await Create().StopAsync(Token);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private RootStateBootstrapHostedService Create() =>
        new(_factory, NullLogger<RootStateBootstrapHostedService>.Instance);

    private sealed class FailingFactory : IDbContextFactory<WaifuDbContext>
    {
        public WaifuDbContext CreateDbContext() =>
            throw new InvalidOperationException("база недоступна");

        public Task<WaifuDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("база недоступна");
    }
}
