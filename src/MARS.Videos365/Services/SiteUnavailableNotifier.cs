using System.Net;
using MARS.Shared.Configuration;
using MARS.Shared.Models;
using Microsoft.Extensions.Options;

namespace MARS.Videos365.Services;

/// <summary>
/// Уведомляет администраторов о недоступности сайта-источника.
/// </summary>
/// <remarks>
/// Список адресов берётся из секции <c>Telegram</c> — того же типа
/// <see cref="TelegramConfig"/>, которым пользуются остальные сервисы репозитория.
/// Мессенджер передаётся nullable: сервис запускается и без Telegram-токена, и в
/// этом случае уведомление не уходит, но конвейер продолжает работать.
/// </remarks>
public sealed class SiteUnavailableNotifier(
    ITelegramAdminMessenger? messenger,
    IOptions<TelegramConfig> telegramConfig,
    ILogger<SiteUnavailableNotifier> logger
)
{
    /// <summary>
    /// Отправляет сообщение каждому администратору. Ошибка отправки одному
    /// адресату не отменяет уведомление остальных, а попадает в результат как
    /// уменьшенный счётчик.
    /// </summary>
    public async Task<OperationResult<int>> NotifyAsync(
        Uri site,
        Exception error,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult<int>.Fail("Стартовая ошибка уведомления");
        var admins = telegramConfig.Value.AdminIds ?? [];

        if (messenger is not null && admins.Length > 0 && site is not null && error is not null)
        {
            var message = $"""
                <b>Сайт {site} недоступен!</b>

                Ошибка: {error.Message}
                """;
            var notified = 0;

            foreach (var adminId in admins)
            {
                try
                {
                    await messenger.SendAsync(adminId, message, cancellationToken);
                    notified++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Telegram notification to {AdminId} failed", adminId);
                }
            }

            if (notified > 0)
            {
                result = OperationResult<int>.Ok(notified);
            }
            else
            {
                result = OperationResult<int>.Fail(
                    "Не удалось отправить уведомление ни одному администратору"
                );
            }
        }
        else if (messenger is null)
        {
            logger.LogWarning(
                "Telegram messenger is not configured: notification about {Site} was skipped",
                site
            );
            result = OperationResult<int>.Fail("Telegram-клиент не настроен");
        }
        else if (admins.Length == 0)
        {
            result = OperationResult<int>.Fail("Не задан ни один администратор");
        }
        else
        {
            result = OperationResult<int>.Fail("Не заданы сайт или ошибка");
        }

        return result;
    }
}
