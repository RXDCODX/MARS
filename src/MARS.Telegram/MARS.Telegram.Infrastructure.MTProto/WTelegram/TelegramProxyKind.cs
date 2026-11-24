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
/// Вид прокси для подключения WTelegram.
/// </summary>
public enum TelegramProxyKind
{
    /// <summary>Прямое подключение, без прокси.</summary>
    None = 0,

    /// <summary>MTProto-прокси (tg://proxy).</summary>
    MTProto = 1,

    /// <summary>HTTP CONNECT-прокси (через TcpHandler).</summary>
    Http = 2,
}
