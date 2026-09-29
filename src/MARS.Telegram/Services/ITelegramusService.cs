using Telegram.Bot;
using Telegram.Bot.Types;

namespace MARS.Telegram.Services;

public interface ITelegramusService
{
    Task HandMessage(ITelegramBotClient client, Update update);
}
