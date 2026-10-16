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
    /// <c>redirect_uri</c> обязан совпадать с тем, что был в ссылке авторизации, —
    /// иначе Twitch отвергнет обмен. Раньше здесь передавался относительный путь,
    /// не совпадавший со ссылкой из уведомления, и первый токен было негде получить.
    /// </remarks>
    [HttpGet("user-auth")]
    public async Task<ActionResult<OperationResult<object?>>> TwitchUserAuth(
        [FromQuery] string code
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
