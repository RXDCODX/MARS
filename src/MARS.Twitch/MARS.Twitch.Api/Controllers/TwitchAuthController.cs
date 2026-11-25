using MARS.Shared.Contracts.Models;
using MARS.Twitch.Application.Models;
using MARS.Twitch.Infrastructure.Services;
using MARS.Twitch.Infrastructure.Services.Connection;
using MARS.Twitch.Infrastructure.Services.EventSub;
using MARS.Twitch.Infrastructure.Services.Management;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TwitchAuthController(
    TokenService tokenService,
    ITwitchAPI api,
    TwitchAuthRedirectUriProvider redirectUri,
    ITwitchConnectionState? twitchConnectionState = null,
    EventSubService? eventSubService = null,
    ILogger<TwitchAuthController>? logger = null
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
        try
        {
            if (!string.IsNullOrWhiteSpace(code) && redirectUri.Value is not null)
            {
                var authToken = await ExchangeCodeAsync(code, redirectUri.Value!);

                await tokenService.ApplyNewTokenAsync(
                    authToken.AccessToken,
                    authToken.RefreshToken,
                    authToken.ExpiresIn
                );

                return Redirect("/twitch-auth/callback?status=success");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                // Twitch при отказе пользователя возвращает error=, а не код:
                // называть это «код не предоставлен» значит отправить искать
                // причину не туда.
                return Redirect(
                    "/twitch-auth/callback?status=error&message="
                        + Uri.EscapeDataString(
                            $"Twitch отклонил авторизацию: {error}. Повторите вход по ссылке из уведомления."
                        )
                );
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return Redirect(
                    "/twitch-auth/callback?status=error&message="
                        + Uri.EscapeDataString("Код авторизации не предоставлен")
                );
            }

            return Redirect(
                "/twitch-auth/callback?status=error&message="
                    + Uri.EscapeDataString(
                        "Не удалось определить адрес обработчика: задайте Twitch__AuthRedirectUri"
                    )
            );
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка при авторизации Twitch пользователя");
            return Redirect(
                "/twitch-auth/callback?status=error&message="
                    + Uri.EscapeDataString("Ошибка при авторизации")
            );
        }
    }

    /// <summary>
    /// Обмен кода на токен. Вынесен в отдельный метод: метод TwitchLib не
    /// виртуальный, и Moq не может его перехватить — через этот шов тесты
    /// подставляют ответ без реального обращения к Twitch.
    /// </summary>
    protected virtual async Task<(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn
    )> ExchangeCodeAsync(string code, string redirectUriValue)
    {
        var authToken = await api.Auth.GetAccessTokenFromCodeAsync(
            code,
            api.Settings.Secret,
            redirectUriValue,
            api.Settings.ClientId
        );
        return (authToken.AccessToken, authToken.RefreshToken, authToken.ExpiresIn);
    }

    /// <summary>
    /// Статус авторизации и подключения Twitch: состояние чата, EventSub и токена.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<OperationResult<TwitchAuthStatusResponse>>> GetStatus()
    {
        try
        {
            var token = await tokenService.GetTokenAsync(CancellationToken.None);

            var status = new TwitchAuthStatusResponse
            {
                IsChatConnected = twitchConnectionState?.IsConnected ?? false,
                IsEventSubConnected = eventSubService?.IsWebSocketConnected ?? false,
                JoinedChannels = twitchConnectionState?.JoinedChannels ?? string.Empty,
                LastConnectedAt = twitchConnectionState?.LastConnectedAt,
                LastDisconnectedAt = twitchConnectionState?.LastDisconnectedAt,
                LastDisconnectReason = twitchConnectionState?.LastDisconnectReason,
                ReconnectAttempts = twitchConnectionState?.ReconnectAttempts ?? 0,
                IsReconnecting = twitchConnectionState?.IsReconnecting ?? false,
                HasToken = token is not null,
                TokenWhenCreated = token?.WhenCreated,
                TokenExpiresIn = token?.ExpiresIn,
                IsTokenExpired = token is null || !TokenService.IsTokenFresh(token),
            };

            return Ok(OperationResult<TwitchAuthStatusResponse>.Ok(status));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка при получении статуса Twitch");
            return Ok(
                OperationResult<TwitchAuthStatusResponse>.Fail("Ошибка при получении статуса")
            );
        }
    }

    /// <summary>
    /// Принудительное переподключение к чату Twitch.
    /// </summary>
    /// <summary>
    /// Возвращает URL для авторизации через Twitch OAuth.
    /// </summary>
    [HttpGet("authorize-url")]
    public ActionResult<OperationResult<string>> GetAuthorizeUrl()
    {
        try
        {
            var clientId = api.Settings.ClientId;
            var redirectUriValue = redirectUri.Value;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUriValue))
            {
                return Ok(OperationResult<string>.Fail("Не настроен ClientId или redirect_uri"));
            }

            // Единый источник прав: тот же набор, что и в ссылке из
            // Telegram-уведомления (TwitchAuthorizationLink), иначе токен,
            // полученный через UI, приходит с урезанными правами.
            var url = TwitchAuthorizationLink.Build(clientId, redirectUriValue!);

            return Ok(OperationResult<string>.Ok(url));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка при формировании URL авторизации");
            return Ok(OperationResult<string>.Fail("Ошибка при формировании URL авторизации"));
        }
    }

    [HttpPost("reconnect")]
    public async Task<ActionResult<OperationResult<string>>> Reconnect()
    {
        try
        {
            if (twitchConnectionState is TwitchConnectionManager manager)
            {
                await manager.ReconnectAsync();
            }
            else
            {
                return Ok(OperationResult<string>.Fail("Менеджер подключения недоступен"));
            }

            return Ok(OperationResult<string>.Ok("Переподключение запущено"));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка при переподключении к Twitch");
            return Ok(OperationResult<string>.Fail("Ошибка при переподключении"));
        }
    }
}
