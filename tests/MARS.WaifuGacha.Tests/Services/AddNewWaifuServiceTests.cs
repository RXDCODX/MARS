using MARS.Shared.Clients;
using MARS.Shared.Concurrency;
using MARS.Shared.Models;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Добавление супруга по ссылке на Shikimori.
///
/// Проверяется разбор ссылки и плата за добавление: она идёт из счётчика
/// гарантии только у обычных пользователей, а VIP-у гарантия не нужна — он и так
/// получает вайфу всегда.
/// </summary>
public class AddNewWaifuServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly Mock<IShikimoriApiClient> _shikimori = new();
    private readonly AddNewWaifuService _service;

    public AddNewWaifuServiceTests()
    {
        var helper = new WaifuRollEnsurenceService(
            NullLogger<WaifuRollEnsurenceService>.Instance,
            _shikimori.Object,
            _factory
        );
        var roll = new WaifuRollService(
            _factory,
            NullLogger<WaifuRollService>.Instance,
            helper,
            new KeyedAsyncLock(),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            )
        );

        _service = new AddNewWaifuService(
            NullLogger<AddNewWaifuService>.Instance,
            _shikimori.Object,
            roll,
            helper,
            new WaifuRollGuaranteeService(_factory, NullLogger<WaifuRollGuaranteeService>.Instance)
        );
    }

    /// <summary>
    /// Id вытаскивается из любой ссылки, где есть <c>characters/число</c>:
    /// зритель присылает и каноническую ссылку, и вложенную внутрь аниме.
    /// Проверяется именно разбор — о нём и говорит вызов Shikimori с нужным id.
    /// </summary>
    [Theory]
    [InlineData("https://shikimori.one/characters/12345-naruto", 12345)]
    [InlineData("https://shikimori.one/animes/z1-title/characters/777-marker", 777)]
    [InlineData("characters/42", 42)]
    public async Task CharacterIdIsTakenFromLink(string link, long expectedId)
    {
        SetupCharacter(expectedId, "Аква");

        var result = await _service.AddNewWaifuAsync(link, "123456789", "Pyro", isVip: false);

        Assert.True(result.Success);
        // Персонажа могут перезапрашивать и сервисы заполнения полей,
        // поэтому проверяется сам факт запроса с разобранным id, а не его
        // количество.
        _shikimori.Verify(
            instance => instance.GetCharacterAsync(expectedId, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );
        Assert.Equal(expectedId.ToString(), result.Result!.Waifu!.ShikiId);
    }

    [Theory]
    [InlineData("не ссылка")]
    [InlineData("https://shikimori.one/characters/без-цифр")]
    [InlineData("")]
    public async Task BrokenLinkIsRejected(string link)
    {
        var result = await _service.AddNewWaifuAsync(link, "123456789", "Pyro", isVip: false);

        Assert.False(result.Success);
        Assert.Contains("кривая ссылка", result.ErrorMessage);
    }

    /// <summary>
    /// Персонаж, которого нет на Shikimori, отличается от кривой ссылки: в
    /// первом случае ссылка верная, а во втором персонажа не существует.
    /// </summary>
    [Fact]
    public async Task UnknownCharacterIsReported()
    {
        _shikimori
            .Setup(instance => instance.GetCharacterAsync(12345L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShikimoriCharacterRef?)null);

        var result = await _service.AddNewWaifuAsync(
            "https://shikimori.one/characters/12345-naruto",
            "123456789",
            "Pyro",
            isVip: false
        );

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DuplicateCharacterIsReported()
    {
        await SeedAsync("12345");
        SetupCharacter(12345, "Аква");

        var result = await _service.AddNewWaifuAsync(
            "https://shikimori.one/characters/12345-aqua",
            "123456789",
            "Pyro",
            isVip: false
        );

        Assert.False(result.Success);
    }

    /// <summary>
    /// Добавление тратит один ролл из гарантии, поэтому до неё остаётся 199:
    /// это число показывается пользователю в ответе.
    /// </summary>
    [Fact]
    public async Task NormalUserPaysOneRollFromGuarantee()
    {
        SetupCharacter(12345, "Аква");

        var result = await _service.AddNewWaifuAsync(
            "https://shikimori.one/characters/12345-aqua",
            "123456789",
            "Pyro",
            isVip: false
        );

        Assert.Equal(199, result.Result!.RollsUntilGuarantee);
    }

    [Fact]
    public async Task VipDoesNotSpendGuarantee()
    {
        SetupCharacter(12345, "Аква");

        var result = await _service.AddNewWaifuAsync(
            "https://shikimori.one/characters/12345-aqua",
            "123456789",
            "Pyro",
            isVip: true
        );

        Assert.Equal(0, result.Result!.RollsUntilGuarantee);
        Assert.False(result.Result.VipDropped);
    }

    private void SetupCharacter(long id, string name) =>
        _shikimori
            .Setup(instance => instance.GetCharacterAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ShikimoriCharacterRef(
                    id,
                    name,
                    name,
                    null,
                    "https://example.org/a.png",
                    "/a.png",
                    null,
                    null
                )
            );

    private async Task SeedAsync(string shikiId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.Waifus.Add(
            new Waifu
            {
                ShikiId = shikiId,
                Name = "Уже есть",
                ImageUrl = "https://example.org/old.png",
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
