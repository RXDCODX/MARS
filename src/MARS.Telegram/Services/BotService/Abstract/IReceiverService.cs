using MARS.Telegram.Data;

namespace MARS.Telegram.Services.BotService.Abstract;

/// <summary>
///     A marker interface for Update Receiver service
/// </summary>
public interface IReceiverService
{
    Task ReceiveAsync(ChatDbContext chatDbContext, CancellationToken stoppingToken);
}
