using System.Collections.Concurrent;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.BlackList;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;
using GetUsers = TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Чёрный список Twitch: пользователи, чьи сообщения не доходят до команд и
/// автоответов.
///
/// Проверяется, что флаг в базе и список в памяти не расходятся: валидатор
/// смотрит именно в <c>TwitchConstants.BlackListedUserIds</c>, и рассинхрон
/// означал бы, что забаненный всё равно отвечает на команды.
/// </summary>
public class TwitchBlackListServiceTests : IDisposable
{
    private readonly ConcurrentBag<string> _original = TwitchConstants.BlackListedUserIds;
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly Mock<ITwitchUserEnsureService> _ensure = new();
    private readonly Mock<ITwitchAPI> _api = new();
    private readonly TwitchBlackListService _service;

    public TwitchBlackListServiceTests()
    {
        _ensure
            .Setup(instance =>
                instance.EnsureUserExistsAsync(
                    It.IsAny<TwitchUser?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns<TwitchUser?, CancellationToken>(
                (user, _) =>
                    Task.FromResult(
                        user
                            ?? new TwitchUser
                            {
                                TwitchId = "123456789",
                                UserLogin = "login",
                                DisplayName = "Pyro",
                            }
                    )
            );
        _ensure
            .Setup(instance =>
                instance.EnsureUserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .Returns<string, CancellationToken>(
                (id, _) =>
                    Task.FromResult(
                        new TwitchUser
                        {
                            TwitchId = id,
                            UserLogin = "login",
                            DisplayName = "Pyro",
                        }
                    )
            );

        _service = new TwitchBlackListService(
            _factory,
            NullLogger<TwitchBlackListService>.Instance,
            _api.Object,
            _ensure.Object,
            new TestLifetime()
        );
    }

    /// <summary>
    /// Список в памяти статический и общий для процесса: без восстановления
    /// тест оставил бы чужим тестам свой чёрный список.
    /// </summary>
    public void Dispose() => TwitchConstants.BlackListedUserIds = _original;

    [Fact]
    public async Task UserIsAddedByTwitchId()
    {
        await SeedAsync("123456789");

        var user = await _service.AddTwitchBlacklistedUserAsync(
            "123456789",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(user!.IsInBlockList);
        Assert.Contains("123456789", TwitchConstants.BlackListedUserIds);
    }

    /// <summary>
    /// Недоступный Twitch не превращается в исключение: команда разбана получит
    /// «пользователь не найден» вместо падения, а в лог уйдёт причина.
    /// </summary>
    [Fact]
    public async Task UnavailableTwitchYieldsNoUser()
    {
        var user = await _service.AddTwitchBlacklistedUserAsync(
            "nobody",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Null(user);
    }

    [Fact]
    public async Task UserIsRemovedFromBlackList()
    {
        await SeedAsync("123456789", blocked: true);

        var user = await _service.RemoveTwitchBlacklistedUserAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.False(user!.IsInBlockList);
        Assert.DoesNotContain("123456789", TwitchConstants.BlackListedUserIds);
    }

    /// <summary>
    /// Пустой вход роняется до похода в базу и Twitch: иначе команда разбана
    /// без аргумента тихо «успешно» ничего бы не сделала.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankInputIsRejected(string? input)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.AddTwitchBlacklistedUserAsync(
                input,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Пользователя нет в базе — возвращается null, а не создаётся запись из
    /// одного лишь ответа Twitch: состав полей тогда был бы неполным.
    /// </summary>
    [Fact]
    public async Task UserMissingInDatabaseYieldsNull()
    {
        var user = await _service.AddTwitchBlacklistedUserAsync(
            "987654321",
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Null(user);
    }

    [Fact]
    public async Task StartDoesNotThrow()
    {
        await SeedAsync("123456789", blocked: true);

        await _service.StartAsync(TestContext.Current.CancellationToken);
        await _service.StopAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedAsync(string twitchId, bool blocked = false)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.TwitchUsers.Add(
            new TwitchUser
            {
                TwitchId = twitchId,
                UserLogin = "login",
                DisplayName = "Pyro",
                IsInBlockList = blocked,
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
