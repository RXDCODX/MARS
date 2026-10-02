using System.Reflection;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Хранение токена Twitch.
///
/// Сервис сам поднимает токен при старте и повторяет попытку, если Twitch временно
/// недоступен: без этого весь сервис остался бы без событий до перезапуска. Токен
/// обновляется только по требованию — самопроизвольная смена токена отозвала бы
/// авторизацию.
/// </summary>
public class TokenServiceStartTests
{
    private readonly TwitchTestDbContextFactory _factory = new();

    /// <summary>
    /// Токен из базы поднимается при старте: без него сервис не обращается к Twitch
    /// вообще.
    /// </summary>
    [Fact]
    public async Task StartupLoadsTokenFromDatabase()
    {
        var factory = new TwitchTestDbContextFactory();
        await using (
            var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            db.TwitchToken.Add(
                new TokenInfo
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    ExpiresIn = TimeSpan.FromHours(1),
                    WhenCreated = DateTime.Now,
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var service = new TokenService(
            Mock.Of<ITwitchAPI>(),
            NullLogger<TokenService>.Instance,
            factory
        );

        await ExecuteAsync(service, TestContext.Current.CancellationToken);

        Assert.Equal("access-token", service.Token!.AccessToken);
    }

    /// <summary>
    /// Новый токен заменяет старый, а не добавляется вторым: иначе сервис держал бы
    /// в базе отозванные токены и выбрал бы не тот.
    /// </summary>
    [Fact]
    public async Task NewTokenReplacesStoredOne()
    {
        await SeedAsync("старый токен");
        var service = Create();

        await service.ApplyNewTokenAsync("новый токен", "новый refresh", 3600);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var stored = db.TwitchToken.AsNoTracking().Single();

        Assert.Equal("новый токен", stored.AccessToken);
        Assert.Equal(TimeSpan.FromSeconds(3600), stored.ExpiresIn);
    }

    /// <summary>
    /// Токен без прежнего кладётся как есть: обновлять нечего.
    /// </summary>
    [Fact]
    public async Task FirstTokenIsStoredAsIs()
    {
        var service = Create();

        await service.ApplyNewTokenAsync("первый токен", "первый refresh", 3600);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal("первый токен", db.TwitchToken.AsNoTracking().Single().AccessToken);
    }

    /// <summary>
    /// Старт без токена и без Twitch не бросает наружу: сервис повторяет попытку
    /// позже, а не падает вместе с хостом.
    /// </summary>
    [Fact]
    public async Task StartupWithoutTokenDoesNotThrow()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await ExecuteAsync(Create(), stopping.Token);
    }

    private TokenService Create() =>
        new(Mock.Of<ITwitchAPI>(), NullLogger<TokenService>.Instance, _factory);

    private async Task SeedAsync(string accessToken)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.TwitchToken.Add(
            new TokenInfo
            {
                AccessToken = accessToken,
                RefreshToken = "refresh",
                ExpiresIn = TimeSpan.FromHours(1),
                WhenCreated = DateTime.Now,
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Task ExecuteAsync(TokenService service, CancellationToken stoppingToken)
    {
        var method = typeof(TokenService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        return (Task)method.Invoke(service, [stoppingToken])!;
    }
}
