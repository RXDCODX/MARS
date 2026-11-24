namespace MARS.Telegram.Application.Configuration;

/// <summary>
/// Способ подключения клиента WTelegram к Telegram.
/// </summary>
public enum TelegramProxyMode
{
    /// <summary>Прямое подключение, без прокси.</summary>
    None = 0,

    /// <summary>Локальный контейнер tg-ws-proxy (MTProto на входе).</summary>
    TgWsProxy = 1,

    /// <summary>Внешний полноценный MTProto-прокси.</summary>
    MTProto = 2,

    /// <summary>HTTP CONNECT-прокси.</summary>
    Http = 3,
}
