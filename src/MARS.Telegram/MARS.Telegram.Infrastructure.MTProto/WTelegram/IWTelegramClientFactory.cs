using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Infrastructure.Postgres.Data;
using MARS.Telegram.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Telegram.Infrastructure.MTProto.WTelegram;

/// <summary>
/// Сборка клиента WTelegram: учётные данные приложения, прокси и сессия в БД.
/// </summary>
public interface IWTelegramClientFactory
{
    /// <summary>
    /// Ждёт ли вход код подтверждения: клиент упёрся в него и без него не пойдёт
    /// дальше.
    /// </summary>
    /// <remarks>
    /// Признак ставится по ошибке входа, а не по факту отправки кода: присланный
    /// код может оказаться не тем, и клиент попросит новый.
    /// </remarks>
    bool AwaitingCode { get; }

    /// <summary>
    /// Сообщение последней неудачи входа — его показывает страница статуса.
    /// </summary>
    /// <remarks>
    /// Без него администратор видел бы только крутилки и не смог бы отличить
    /// «неверный код» от «прокси недоступен».
    /// </remarks>
    string? LastLoginError { get; }

    /// <summary>
    /// План подключения к Telegram: вид прокси, адрес MTProto-прокси (если
    /// применимо) либо HTTP-эндпоинт (если применимо), а также строка для
    /// журнала. При режиме <see cref="TelegramProxyMode.None"/> возвращает план
    /// с видом <see cref="TelegramProxyKind.None"/> и пустыми адресами — клиент
    /// идёт в Telegram напрямую.
    /// </summary>
    /// <remarks>
    /// Для MTProto-режимов (TgWsProxy/MTProto) при заданном адресе и отсутствии
    /// секрета бросает <see cref="InvalidOperationException"/>: без секрета
    /// MTProto-прокси не примет подключение, а WTelegram падает внутри себя с
    /// <see cref="ArgumentNullException"/> на <c>Enumerable.All</c>.
    /// </remarks>
    TelegramProxyPlan ResolveProxy();

    /// <summary>
    /// Код подтверждения, присланный снаружи (с веб-страницы). Уходит в клиент при
    /// следующей попытке входа и главнее значения из конфигурации.
    /// </summary>
    void SubmitLoginCode(string code);

    /// <summary>Запоминает, чем кончилась попытка входа.</summary>
    void NoteLoginFailed(Exception error);

    /// <summary>Снимает ожидание кода: вход состоялся.</summary>
    void NoteLoginSucceeded();

    /// <summary>
    /// Клиент с сессией из указанного потока, без входа в аккаунт.
    /// </summary>
    WTelegramClient Create(Stream sessionStore);

    /// <summary>Клиент, вошедший как бот по токену.</summary>
    Task<WTelegramClient> LoginAsBotAsync(string botToken, CancellationToken cancellationToken);

    /// <summary>
    /// Клиент, вошедший как бот по токену, с возможностью настроить его до входа
    /// (например, подключить <c>UpdateManager</c>).
    /// </summary>
    Task<WTelegramClient> LoginAsBotAsync(
        string botToken,
        CancellationToken cancellationToken,
        Action<WTelegramClient>? configure
    );

    /// <summary>Клиент, вошедший как пользователь по номеру телефона.</summary>
    Task<WTelegramClient> LoginAsUserAsync(CancellationToken cancellationToken);
}
