using System;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Twitch.Api.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;

namespace MARS.Twitch.Api.Services.Management;

/// <summary>
/// Просьба пройти авторизацию через <c>MARS.Telegram</c>.
/// </summary>
/// <remarks>
/// <para>
/// Бот принадлежит <c>MARS.Telegram</c>, и сообщение администраторам отправляет он:
/// свой клиент Telegram в этом сервисе означал бы двух владельцев одного токена и
/// переменные бота в двух блоках compose.
/// </para>
/// <para>
/// Отказ не роняет сервис: <see cref="TokenService"/> зовёт нотификатор из фоновой
/// службы старта, и недоступность Telegram не должна превращаться в исключение
/// на каждой попытке. Всё, что здесь может пойти не так, попадает в лог.
/// </para>
/// </remarks>
public class TelegramAuthNotifier(
    ITelegramAdminClient telegram,
    TwitchAuthRedirectUriProvider redirectUri,
    ITwitchAPI api,
    ILogger<TelegramAuthNotifier> logger
) : ITwitchAuthNotifier
{
    /// <inheritdoc />
    public async Task<bool> NotifyMissingTokenAsync()
    {
        try
        {
            if (redirectUri.Value is not { } handler)
            {
                logger.LogWarning(
                    "Ссылка авторизации Twitch не отправлена: неизвестен адрес обработчика "
                        + "(задайте Twitch__AuthRedirectUri)."
                );

                return false;
            }

            var authUrl = TwitchAuthorizationLink.Build(api.Settings.ClientId, handler);

            // null означает отказ сервиса: не настроен, недоступен или не доставил
            // ни одному администратору. Причина уже записана клиентом.
            var reply = await telegram.SendToAdminsAsync(
                service: "MARS.TwitchCore",
                message:
                    "<b>Нужно пройти переаунтефикацию для твича!</b>\n\n"
                    + $"<a href=\"{authUrl}\">🔗 Нажмите здесь для авторизации в Twitch</a>",
                cancellationToken: CancellationToken.None
            );

            if (reply is null)
            {
                logger.LogWarning(
                    "MARS.Telegram не доставил просьбу пройти авторизацию Twitch"
                );

                return false;
            }

            logger.LogInformation(
                "Просьба пройти авторизацию Twitch отправлена: администраторов {Recipients}",
                reply.Recipients
            );

            return reply.Recipients > 0;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return false;
        }
    }
}
