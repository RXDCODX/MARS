using MARS.Telegram.Application.Models;

namespace MARS.Telegram.Application.Services;

/// <summary>
/// Проверка подключения MTProto-бота (пассивная и активная).
/// </summary>
public interface ITelegramBotStatusService
{
    /// <summary>Текущее состояние без побочных эффектов (не инициирует вход).</summary>
    TelegramBotStatus GetStatus();

    /// <summary>
    /// Активная проверка: при необходимости инициирует вход и возвращает свежий статус.
    /// </summary>
    Task<TelegramBotStatus> CheckAsync(CancellationToken cancellationToken = default);
}
