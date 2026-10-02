using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Настоящий <see cref="TokenService"/> поверх базы в памяти.
///
/// Сервис конкретный и хранит токен в своей таблице, а свежий токен положить
/// больше нечем: у <c>TokenService.Token</c> сеттер внутренний. Мок интерфейса
/// тут не подошёл бы — интерфейса у сервиса нет, а проверяется именно поведение
/// «токен лежит в базе», из которого его читают все вызывающие.
/// </summary>
internal static class TwitchTokenServiceStub
{
    /// <summary>
    /// Сервис без единого токена: все обращения к Twitch должны быть отклонены
    /// до похода в сеть.
    /// </summary>
    public static TokenService WithoutToken() =>
        new(
            Mock.Of<ITwitchAPI>(),
            NullLogger<TokenService>.Instance,
            new TwitchTestDbContextFactory()
        );

    public static async Task<TokenService> WithTokenAsync(
        string token = "access-token",
        CancellationToken cancellationToken = default
    )
    {
        var factory = new TwitchTestDbContextFactory();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.TwitchToken.Add(
            new TokenInfo
            {
                AccessToken = token,
                RefreshToken = "refresh-token",
                ExpiresIn = TimeSpan.FromHours(1),
                WhenCreated = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync(cancellationToken);

        return new TokenService(Mock.Of<ITwitchAPI>(), NullLogger<TokenService>.Instance, factory);
    }
}
