using MARS.TwitchCore.Controllers;
using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Controllers;

/// <summary>
/// <see cref="TwitchRewardsController.AccessToken"/> — приватное свойство,
/// возвращающее токен из <see cref="TokenService"/>. Проверяется через
/// рефлексию, так как у свойства нет public-геттера.
/// </summary>
public class TwitchRewardsControllerTests
{
    [Fact]
    public void AccessTokenReturnsTokenFromService()
    {
        var api = Mock.Of<ITwitchAPI>();
        var factory = new Mock<IDbContextFactory<TwitchDbContext>>();
        var configuration = new ConfigurationBuilder().Build();
        var authNotifier = Mock.Of<ITwitchAuthNotifier>();

        var tokenService = new TokenService(
            api,
            NullLogger<TokenService>.Instance,
            factory.Object,
            configuration,
            authNotifier,
            TimeProvider.System
        );

        // Устанавливаем свежий токен через рефлексию, чтобы не ходить в базу данных
        var tokenInfoField = typeof(TokenService).GetField(
            "_tokenInfo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        var tokenInfo = new TokenInfo
        {
            Id = Guid.NewGuid(),
            AccessToken = "test-access-token",
            RefreshToken = "refresh-token",
            ExpiresIn = TimeSpan.FromHours(1),
            WhenCreated = DateTime.UtcNow,
        };
        tokenInfoField.SetValue(tokenService, tokenInfo);

        var controller = new TwitchRewardsController(
            api,
            tokenService,
            NullLogger<TwitchRewardsController>.Instance
        );

        var accessTokenProperty = typeof(TwitchRewardsController).GetProperty(
            "AccessToken",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        var result = (string?)accessTokenProperty.GetValue(controller);

        Assert.Equal("test-access-token", result);
    }
}
