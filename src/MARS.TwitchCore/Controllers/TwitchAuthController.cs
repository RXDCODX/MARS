using MARS.Shared.Models;
using MARS.TwitchCore.Services;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TwitchAuthController(
    TokenService tokenService,
    ITwitchAPI api,
    ILogger<TwitchAuthController> logger
) : ControllerBase
{
    [HttpGet("user-auth")]
    public async Task<ActionResult<OperationResult<object?>>> TwitchUserAuth(
        [FromQuery] string code
    )
    {
        ActionResult<OperationResult<object?>> result = null!;

        try
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var authToken = await api.Auth.GetAccessTokenFromCodeAsync(
                    code,
                    api.Settings.Secret,
                    "/api/twitchauth/user-auth",
                    api.Settings.ClientId
                );

                await tokenService.ApplyNewTokenAsync(
                    authToken.AccessToken,
                    authToken.RefreshToken,
                    authToken.ExpiresIn
                );

                result = Ok(OperationResult<object?>.Ok(authToken));
            }
            else
            {
                result = Ok(OperationResult<object?>.Fail("Код авторизации не предоставлен"));
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
