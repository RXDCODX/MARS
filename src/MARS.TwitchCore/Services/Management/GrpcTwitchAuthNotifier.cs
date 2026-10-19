using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Services.Management;

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
public class GrpcTwitchAuthNotifier(
    TelegramAdminService.TelegramAdminServiceClient telegram,
    TwitchAuthRedirectUriProvider redirectUri,
    ITwitchAPI api,
    ILogger<GrpcTwitchAuthNotifier> logger
) : ITwitchAuthNotifier
{
    /// <inheritdoc />
    public async Task NotifyMissingTokenAsync()
    {
        try
        {
            if (redirectUri.Value is not { } handler)
            {
                logger.LogWarning(
                    "Ссылка авторизации Twitch не отправлена: неизвестен адрес обработчика "
                        + "(задайте Twitch__AuthRedirectUri)."
                );

                return;
            }

            var authUrl = TwitchAuthorizationLink.Build(api.Settings.ClientId, handler);

            var reply = await telegram.SendToAdminsAsync(
                new AdminMessageRequest
                {
                    Service = "MARS.TwitchCore",
                    Message =
                        "<b>Нужно пройти переаунтефикацию для твича!</b>\n\n"
                        + $"<a href=\"{authUrl}\">🔗 Нажмите здесь для авторизации в Twitch</a>",
                },
                cancellationToken: CancellationToken.None
            );

            logger.LogInformation(
                "Просьба пройти авторизацию Twitch отправлена: администраторов {Recipients}",
                reply.Recipients
            );
        }
        catch (RpcException e)
        {
            // FAILED_PRECONDITION — это ответ самого MARS.Telegram: бот не настроен
            // или пуст список админов. UNAVAILABLE — сервис не поднят или недоступен.
            // Оба случая — повод написать в лог, а не уронить фоновую службу.
            logger.LogWarning(
                e,
                "MARS.Telegram не доставил просьбу пройти авторизацию Twitch ({Status}): {Detail}",
                e.StatusCode,
                e.Status.Detail
            );
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }
}
