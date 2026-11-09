using MARS.Telegram.Application.Models;
using MARS.Telegram.Infrastructure.Integrations.Telegram.PrivateChannelsResender;

namespace MARS.Telegram.Infrastructure.Integrations.Telegram.Services;

public interface IWTelegramClientService
{
    Task<WTelegramClientStatus> GetClientStatusAsync(CancellationToken cancellationToken = default);
    Task ReLoginAsync(CancellationToken cancellationToken = default);
    bool SubmitVerificationCode(string code);
    Task<IWTelegramChannelClient> GetClientAsync(CancellationToken cancellationToken = default);
}
