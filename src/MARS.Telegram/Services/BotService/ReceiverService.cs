using MARS.Telegram.Services.BotService.Abstract;
using global::Telegram.Bot;

namespace MARS.Telegram.Services.BotService;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ILogger<ReceiverServiceBase<UpdateHandler>> logger
) : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);
