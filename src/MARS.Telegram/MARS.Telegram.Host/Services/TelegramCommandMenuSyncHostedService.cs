using MARS.Telegram.Infrastructure.MTProto.BotService;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Telegram.Host.Services;

[ExcludeFromCodeCoverage]
public sealed class TelegramCommandMenuSyncHostedService(ITelegramBotSession botSession) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await botSession.SynchronizeCommandMenuAsync(stoppingToken);
        }
        catch
        {
            // Menu synchronization must not prevent bot startup.
        }
    }
}