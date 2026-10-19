using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwitchLib.Api.Core;
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
    /// Сервис поверх указанной фабрики: конфигурация по умолчанию пустая, то есть
    /// первый токен сервис берёт из базы, а не подставляет из окружения.
    /// </summary>
    public static TokenService Create(
        IDbContextFactory<TwitchDbContext> factory,
        IConfiguration? configuration = null,
        ITwitchAuthNotifier? notifier = null
    )
    {
        // Settings у настоящего API не null, а у loose-мока возвращается null: без
        // него сервис упал бы на записи access token в настройки.
        var api = new Mock<ITwitchAPI>();
        api.SetupGet(instance => instance.Settings).Returns(new ApiSettings());

        return new TokenService(
            api.Object,
            NullLogger<TokenService>.Instance,
            factory,
            configuration ?? new ConfigurationBuilder().Build(),
            notifier ?? new SilentAuthNotifier(),
            TimeProvider.System
        );
    }

    /// <summary>
    /// Нотификатор-заглушка: молчит и ничего не отправляет. В тестах о самом
    /// уведомлении — отдельный набор проверок у Telegram-нотификатора.
    /// </summary>
    private sealed class SilentAuthNotifier : ITwitchAuthNotifier
    {
        public Task NotifyMissingTokenAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// Сервис без единого токена: все обращения к Twitch должны быть отклонены
    /// до похода в сеть.
    /// </summary>
    public static TokenService WithoutToken() => Create(new TwitchTestDbContextFactory());

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

        return Create(factory);
    }
}
