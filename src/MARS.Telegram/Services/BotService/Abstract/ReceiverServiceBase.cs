using global::Telegram.Bot;
using global::Telegram.Bot.Polling;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Services.BotService.Abstract;

/// <summary>
///     An abstract class to compose Receiver Service and Update Handler classes
/// </summary>
/// <typeparam name="TUpdateHandler">Update Handler to use in Update Receiver</typeparam>
public abstract class ReceiverServiceBase<TUpdateHandler> : IReceiverService
    where TUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<ReceiverServiceBase<TUpdateHandler>> _logger;
    private readonly IUpdateHandler _updateHandler;

    internal ReceiverServiceBase(
        ITelegramBotClient botClient,
        TUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<TUpdateHandler>> logger
    )
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _logger = logger;
    }

    public async Task ReceiveAsync(ChatDbContext context, CancellationToken stoppingToken)
    {
        var offset = await GetOffset(context, stoppingToken);
        var isOffsetRequired = offset is not null;

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
            DropPendingUpdates = !isOffsetRequired,
            Offset = offset?.Offset,
        };

        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation(
            "Start receiving updates for {BotName}",
            me.Username ?? "My Awesome Bot"
        );

        await _botClient.ReceiveAsync(_updateHandler, receiverOptions, stoppingToken);
    }

    private static async Task<TelegramUpdateReceiverOffset?> GetOffset(
        ChatDbContext context,
        CancellationToken token
    )
    {
        var offset = await context.TelegramUpdateReceiverOffsets.SingleOrDefaultAsync(
            cancellationToken: token
        );
        return offset;
    }
}
