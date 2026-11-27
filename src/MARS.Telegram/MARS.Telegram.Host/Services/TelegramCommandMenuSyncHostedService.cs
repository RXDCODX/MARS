using MARS.Telegram.Infrastructure.MTProto.BotService;
using Microsoft.Extensions.Hosting;

namespace MARS.Telegram.Host.Services;

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