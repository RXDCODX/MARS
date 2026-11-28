using System.Diagnostics.CodeAnalysis;
namespace MARS.Telegram.Application.Services;

public interface ITelegramWhitelistService
{
    Task<IReadOnlyList<string>> GetPyroAlertsUsersAsync(CancellationToken cancellationToken = default);
}