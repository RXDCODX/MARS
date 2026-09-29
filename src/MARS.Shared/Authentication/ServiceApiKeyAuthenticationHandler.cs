using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using MARS.Shared.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Authentication;

/// <summary>
/// Схема аутентификации по общему API-ключу межсервисного общения.
/// Используется Gateway ⇄ сервисы и сервис ⇄ сервис для защиты внутренних REST-эндпоинтов.
/// </summary>
public class ServiceApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ServiceAuthOptions> authOptions
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ServiceApiKey";

    /// <summary>
    /// Альтернативное имя заголовка, документированное в
    /// <see cref="ServiceAuthOptions"/> (некоторые клиенты присылают <c>Api-Key</c>).
    /// </summary>
    private const string AlternativeApiKeyHeaderName = "Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = authOptions.Value;

        if (!settings.IsEnabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = ReadProvidedKey(settings);

        if (providedKey is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var expectedKey = settings.ApiKey ?? string.Empty;

        var matches =
            providedKey.Length == expectedKey.Length
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedKey),
                Encoding.UTF8.GetBytes(expectedKey)
            );

        if (matches)
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "mars-service"),
                    new Claim(ClaimTypes.NameIdentifier, "service-api-key"),
                ],
                SchemeName
            );

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = SchemeName;

        return Task.CompletedTask;
    }

    private string? ReadProvidedKey(ServiceAuthOptions settings)
    {
        string? result = null;

        string[] headerNames = string.Equals(
            settings.ApiKeyHeaderName,
            AlternativeApiKeyHeaderName,
            StringComparison.OrdinalIgnoreCase
        )
            ? [settings.ApiKeyHeaderName]
            : [settings.ApiKeyHeaderName, AlternativeApiKeyHeaderName];

        foreach (var headerName in headerNames)
        {
            if (Request.Headers.TryGetValue(headerName, out var provided))
            {
                result = provided.ToString().Trim();
                break;
            }
        }

        return result;
    }
}
