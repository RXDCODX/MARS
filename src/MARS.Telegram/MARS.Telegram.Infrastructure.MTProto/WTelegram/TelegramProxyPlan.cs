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
/// План подключения клиента WTelegram к Telegram.
/// </summary>
public sealed record TelegramProxyPlan(
    TelegramProxyKind Kind,
    string? MTProxyUrl,
    string? HttpHost,
    int? HttpPort,
    string? HttpUser,
    string? HttpPassword,
    string Description
)
{
    public static TelegramProxyPlan NonePlan(string description) =>
        new(TelegramProxyKind.None, null, null, null, null, null, description);
}
