using MARS.Telegram.Application.Models;

namespace MARS.Telegram.Application.Services;

/// <summary>Управление авторизацией пользовательского Telegram-аккаунта.</summary>
public interface IWTelegramClientService
{
    Task<WTelegramClientStatus> GetClientStatusAsync(CancellationToken cancellationToken = default);
    Task ReLoginAsync(CancellationToken cancellationToken = default);
    bool SubmitVerificationCode(string code);
}
