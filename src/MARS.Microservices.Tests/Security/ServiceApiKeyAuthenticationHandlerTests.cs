using System.Text.Encodings.Web;
using MARS.Shared.Authentication;
using MARS.Shared.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.Microservices.Tests.Security;

/// <summary>
/// Блокер №6: схема <c>ServiceApiKey</c> была зарегистрирована во всех сервисах,
/// но не навешивалась ни на один эндпоинт. Тесты фиксируют контракт схемы,
/// на который теперь опираются <c>[Authorize]</c> в MARS.Admin и
/// <c>AuthorizationPolicy</c> в YARP-маршрутах Gateway:
/// нет корректного ключа — аутентификации нет, то есть 401, а не 200.
/// </summary>
public class ServiceApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task HandleAuthenticateAsync_WhenApiKeyMatches_ThenSuccess()
    {
        var (handler, context) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = "correct-key",
        });
        context.Request.Headers[new ServiceAuthOptions().ApiKeyHeaderName] = "correct-key";

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("mars-service", result.Principal?.Identity?.Name);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenAlternativeHeaderUsed_ThenSuccess()
    {
        // ServiceAuthOptions документирует заголовок "Api-Key" как допустимый.
        var (handler, context) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = "correct-key",
        });
        context.Request.Headers["Api-Key"] = "correct-key";

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenApiKeyIsWrong_ThenFail()
    {
        var (handler, context) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = "correct-key",
        });
        context.Request.Headers[new ServiceAuthOptions().ApiKeyHeaderName] = "wrong-key";

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Failure is not null);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenHeaderMissing_ThenNotAuthenticated()
    {
        var (handler, _) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = "correct-key",
        });

        var result = await handler.AuthenticateAsync();

        Assert.True(AuthenticateResult.NoResult().Equals(result));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenKeyNotConfigured_ThenNotAuthenticated()
    {
        // Пустой ключ = схема выключена. Схема обязана быть fail-closed:
        // иначе админские эндпоинты снова окажутся без аутентификации.
        var (handler, context) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = string.Empty,
        });
        context.Request.Headers[new ServiceAuthOptions().ApiKeyHeaderName] = "anything";

        var result = await handler.AuthenticateAsync();

        Assert.True(AuthenticateResult.NoResult().Equals(result));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenKeyHasDifferentLength_ThenFail()
    {
        // Сравнение по длине и constant-time: ключ другого размера
        // не должен подбираться подоль.
        var (handler, context) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = "correct-key",
        });
        context.Request.Headers[new ServiceAuthOptions().ApiKeyHeaderName] = "correct-ke";

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Failure is not null);
    }

    [Fact]
    public async Task HandleChallengeAsync_WhenInvoked_ThenReturns401()
    {
        var (handler, context) = await CreateHandlerAsync(new ServiceAuthOptions
        {
            ApiKey = "correct-key",
        });

        await handler.ChallengeAsync(properties: null);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    private static async Task<
        (ServiceApiKeyAuthenticationHandler Handler, DefaultHttpContext Context)
    > CreateHandlerAsync(ServiceAuthOptions options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor
            .Setup(monitor => monitor.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());

        var handler = new ServiceApiKeyAuthenticationHandler(
            optionsMonitor.Object,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(options)
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await handler.InitializeAsync(
            new AuthenticationScheme(
                ServiceApiKeyAuthenticationHandler.SchemeName,
                null,
                typeof(ServiceApiKeyAuthenticationHandler)
            ),
            context
        );

        return (handler, context);
    }
}
