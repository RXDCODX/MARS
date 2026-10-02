using MARS.Telegram.Entities;

namespace MARS.Telegram.Services;

public interface IWTelegramClientService
{
    Task<WTelegramClientStatus> GetClientStatusAsync(CancellationToken cancellationToken = default);
    Task ReLoginAsync(CancellationToken cancellationToken = default);
    bool SubmitVerificationCode(string code);
    Task<WTelegramClient> GetClientAsync(CancellationToken cancellationToken = default);
    Task HandleUpdate(
        global::Telegram.Bot.ITelegramBotClient _,
        global::Telegram.Bot.Types.Update? update
    );
}
