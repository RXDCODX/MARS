using MARS.Telegram.Data;
using MARS.Telegram.Services.BotService.Abstract;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Services.BotService;

// Compose Polling and ReceiverService implementations
public class PollingService(
    IServiceProvider serviceProvider,
    ILogger<PollingService> logger,
    IDbContextFactory<ChatDbContext> factory
) : PollingServiceBase<ReceiverService>(serviceProvider, logger, factory);
