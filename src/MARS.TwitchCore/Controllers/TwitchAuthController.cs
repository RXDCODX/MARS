using MARS.Shared.Models;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.Management;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TwitchAuthController(
    TokenService tokenService,
    ITwitchAPI api,
    TwitchAuthRedirectUriProvider redirectUri,
    ILogger<TwitchAuthController> logger
) : ControllerBase
{
    /// <summary>
    /// Обмен кода на токен.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>redirect_uri</c> обязан совпадать с тем, что был в ссылке авторизации, —
    /// иначе Twitch отвергнет обмен. Раньше здесь передавался относительный путь,
    /// не совпадавший со ссылкой из уведомления, и первый токен было негде получить.
    /// </para>
    /// <para>
    /// Параметры объявлены через <c>string?</c> намеренно: с непустым
    /// <c>string</c> атрибут <c>[ApiController]</c> отвечает 400
    /// <c>ValidationProblemDetails</c> («The code field is required.») <em>до</em>
    /// тела метода, и проверки внутри недостижимы — бизнес-ошибка уходит наружу
    /// чужой и невнятной.
    /// </para>
    /// </remarks>
    [HttpGet("user-auth")]
    public async Task<ActionResult<OperationResult<object?>>> TwitchUserAuth(
        [FromQuery] string? code,
        [FromQuery] string? error = null
    )
    {
        ActionResult<OperationResult<object?>> result = null!;

        try
        {
            if (!string.IsNullOrWhiteSpace(code) && redirectUri.Value is not null)
            {
                var authToken = await api.Auth.GetAccessTokenFromCodeAsync(
                    code,
                    api.Settings.Secret,
                    redirectUri.Value,
                    api.Settings.ClientId
                );

                await tokenService.ApplyNewTokenAsync(
                    authToken.AccessToken,
                    authToken.RefreshToken,
                    authToken.ExpiresIn
                );

                result = Ok(OperationResult<object?>.Ok(authToken));
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                // Twitch при отказе пользователя возвращает error=, а не код:
                // называть это «код не предоставлен» значит отправить искать
                // причину не туда.
                result = Ok(
                    OperationResult<object?>.Fail(
                        $"Twitch отклонил авторизацию: {error}. Повторите вход по ссылке из уведомления."
                    )
                );
            }
            else if (string.IsNullOrWhiteSpace(code))
            {
                result = Ok(OperationResult<object?>.Fail("Код авторизации не предоставлен"));
            }
            else
            {
                result = Ok(
                    OperationResult<object?>.Fail(
                        "Не удалось определить адрес обработчика: задайте Twitch__AuthRedirectUri"
                    )
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при авторизации Twitch пользователя");
            result = Ok(OperationResult<object?>.Fail("Ошибка при авторизации"));
        }

        return result;
    }
}
