using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.Shikimori.Controllers;
using MARS.Shikimori.Data;
using MARS.Shikimori.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shikimori.Tests.Services;

/// <summary>
/// Внутренний API Shikimori.
///
/// Проверяется конверт ответа и работа с локальной базой. Сам Shikimori в тесте
/// недоступен (сети нет), поэтому проверяется отказная ветка: наружу уходит
/// <c>OperationResult</c> с сообщением, а не исключение, и HTTP 500 — тоже не
/// подходит, потому что вызывающий сервис ждёт конверт.
/// </summary>
public class ShikimoriControllerTests
{
    private readonly ShikimoriTestDbContextFactory _factory = new();
    private readonly ShikimoriController _controller;

    public ShikimoriControllerTests()
    {
        var service = new ShikimoriService(
            NullLogger<ShikimoriService>.Instance,
            Options.Create(new ShikimoriClientOptions()),
            new StubRateLimiter(),
            new StubClient()
        );

        _controller = new ShikimoriController(
            service,
            new ShikimoriCatalog(_factory, NullLogger<ShikimoriCatalog>.Instance),
            NullLogger<ShikimoriController>.Instance
        );
    }

    [Fact]
    public async Task UnavailableAnimeIsReportedAsError()
    {
        var response = await _controller.GetRandomAnime(TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.False(Envelope(response).Success);
    }

    [Fact]
    public async Task UnavailableMangaIsReportedAsError()
    {
        var response = await _controller.GetRandomManga(TestContext.Current.CancellationToken);

        Assert.False(Envelope(response).Success);
    }

    /// <summary>
    /// Неположительный id отбрасывается до обращения к Shikimori: это ошибка
    /// запроса, а не отказ внешнего сервиса.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task NonPositiveCharacterIdIsRejected(long id)
    {
        var response = await _controller.GetCharacter(id, TestContext.Current.CancellationToken);

        Assert.False(Envelope(response).Success);
    }

    [Fact]
    public async Task UnavailableCharacterIsReportedAsError()
    {
        var response = await _controller.GetCharacter(42, TestContext.Current.CancellationToken);

        Assert.False(Envelope(response).Success);
    }

    /// <summary>
    /// Персонаж, которого ещё нет в базе, отдаётся ошибкой с понятным текстом:
    /// это штатная ситуация после перезапуска, а не поломка.
    /// </summary>
    [Fact]
    public async Task UnknownCachedCharacterIsReportedAsError()
    {
        var response = await _controller.GetCachedCharacter(
            42,
            TestContext.Current.CancellationToken
        );

        Assert.False(Envelope(response).Success);
    }

    [Fact]
    public async Task CachedCharacterIsReturnedFromDatabase()
    {
        await SeedCharacterAsync(42, "Наруто");

        var response = await _controller.GetCachedCharacter(
            42,
            TestContext.Current.CancellationToken
        );

        Assert.True(Envelope(response).Success);
        Assert.Equal(42, Envelope(response).Result!.Id);
    }

    /// <summary>
    /// Состояние рейт-лимитера отдаётся без обращения к Shikimori: его видит
    /// админ-панель, и он должен отвечать даже при недоступном API.
    /// </summary>
    [Fact]
    public void RateLimiterInfoIsAvailable()
    {
        var response = _controller.GetRateLimiterInfo();

        var envelope =
            Assert.IsType<OkObjectResult>(response.Result).Value
            as OperationResult<ShikimoriRateLimiterInfo>;
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
    }

    private static OperationResult<T> Envelope<T>(ActionResult<OperationResult<T>> response) =>
        Assert.IsType<OkObjectResult>(response.Result).Value is OperationResult<T> envelope
            ? envelope
            : throw new InvalidOperationException("контроллер вернул не OkObjectResult");

    private async Task SeedCharacterAsync(long id, string name)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.Characters.Add(
            new MARS.Shikimori.Entities.ShikimoriCharacter
            {
                Id = id,
                Name = name,
                ImageUrl = "https://example.org/c.png",
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Рейт-лимитер без ограничений: тест проверяет контроллер, а лимит всё
    /// равно не позволяет уйти в сеть — запрос упадёт по таймауту Shikimori.
    /// </summary>
    /// <summary>
    /// Клиент-заглушка вместо ShikimoriSharp: в тесте нет сети, а проверяется
    /// контроллер и работа с локальной базой.
    /// </summary>
    private sealed class StubClient : IShikimoriClient
    {
        public Task<AnimeRef?> GetRandomAnimeAsync(
            int minimumScore,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<AnimeRef?>(null);

        public Task<MangaRef?> GetRandomMangaAsync(
            int minimumScore,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<MangaRef?>(null);

        public Task<CharacterDetails?> GetCharacterAsync(
            long id,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<CharacterDetails?>(null);
    }

    private sealed class StubRateLimiter : IShikimoriRateLimiter
    {
        public Task<bool> TryAcquireAsync() => Task.FromResult(true);

        public Task WaitForSlotAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public RateLimiterInfo GetInfo() =>
            new() { AvailablePerSecond = 3, AvailablePerMinute = 60 };
    }
}
