using MARS.Shared.Contracts.Models;
using MARS.Twitch.Application.Models;
using MARS.Twitch.Application.Services;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Client.Interfaces;

namespace MARS.Twitch.Api.Controllers;

/// <summary>
/// Эндпоинты страницы консоли администратора MARS.Twitch.
/// </summary>
/// <remarks>
/// Доступны без ключа, как Telegram-консоль: служба предполагается защищённой
/// сетью или Gateway. Отправка и подключение каналов выполняются под текущим
/// токеном Twitch.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class TwitchConsoleController(
    ITwitchClient client,
    TwitchUserInfoService userInfoService,
    TwitchInboxStore inbox,
    ILogger<TwitchConsoleController> logger
) : ControllerBase
{
    /// <summary>
    /// Текущий владелец токена Twitch (под кем вошёл).
    /// </summary>
    [HttpGet("identity")]
    public async Task<ActionResult<OperationResult<TwitchConsoleIdentity>>> GetIdentity(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var user = await userInfoService.GetCurrentUserAsync(cancellationToken);

            if (user is null)
            {
                return Ok(
                    OperationResult<TwitchConsoleIdentity>.Fail(
                        "Токен Twitch не найден или недействителен"
                    )
                );
            }

            return Ok(
                OperationResult<TwitchConsoleIdentity>.Ok(
                    new TwitchConsoleIdentity
                    {
                        UserId = user.Id,
                        Login = user.Login,
                        DisplayName = user.DisplayName,
                        ProfileImageUrl = user.ProfileImageUrl,
                    }
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return Ok(
                OperationResult<TwitchConsoleIdentity>.Fail("Ошибка при получении пользователя")
            );
        }
    }

    /// <summary>
    /// Снимок недавно полученных чат-сообщений выбранного канала.
    /// </summary>
    [HttpGet("inbox")]
    public ActionResult<IReadOnlyList<TwitchInboxEntry>> GetInbox(
        [FromQuery] string? channel = null
    )
    {
        return Ok(inbox.Snapshot(channel));
    }

    /// <summary>
    /// Присоединиться к каналу, чтобы получать его сообщения.
    /// </summary>
    [HttpPost("channel/join")]
    public async Task<ActionResult<OperationResult<string>>> JoinChannel(
        [FromBody] TwitchConsoleChannelRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Channel))
        {
            return BadRequest(OperationResult<string>.Fail("Канал не указан"));
        }

        var channel = NormalizeChannel(request.Channel);

        if (string.IsNullOrWhiteSpace(channel) || !IsValidChannel(channel))
        {
            return BadRequest(OperationResult<string>.Fail("Некорректный логин канала"));
        }

        try
        {
            if (!client.IsConnected)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    OperationResult<string>.Fail("Twitch chat is not connected")
                );
            }

            if (
                !client.JoinedChannels.Any(item =>
                    item.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                await client.JoinChannelAsync(channel);
            }

            return Ok(OperationResult<string>.Ok(channel));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                OperationResult<string>.Fail("Не удалось подключиться к каналу")
            );
        }
    }

    /// <summary>
    /// Отправка сообщения в выбранный канал.
    /// </summary>
    [HttpPost("chat/send")]
    public async Task<ActionResult<OperationResult<string>>> SendChatMessage(
        [FromBody] TwitchConsoleSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(OperationResult<string>.Fail("Пустое сообщение"));
        }

        var channel = NormalizeChannel(request.Channel);

        if (string.IsNullOrWhiteSpace(channel) || !IsValidChannel(channel))
        {
            return BadRequest(OperationResult<string>.Fail("Канал не указан"));
        }

        try
        {
            if (!client.IsConnected)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    OperationResult<string>.Fail("Twitch chat is not connected")
                );
            }

            await client.SendMessageToChannelAsync(channel, request.Text, logger);

            return Ok(OperationResult<string>.Ok(channel));
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                OperationResult<string>.Fail("Не удалось отправить сообщение")
            );
        }
    }

    private static string NormalizeChannel(string channel) =>
        channel.Trim().TrimStart('@').ToLowerInvariant();

    private static bool IsValidChannel(string channel) =>
        channel.Length is >= 3 and <= 25 && channel.All(c => char.IsLetterOrDigit(c) || c == '_');
}
