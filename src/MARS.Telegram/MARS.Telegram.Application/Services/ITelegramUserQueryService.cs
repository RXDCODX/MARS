namespace MARS.Telegram.Application.Services;

/// <summary>Запросы к пользователям Telegram без привязки к EF Core.</summary>
public interface ITelegramUserQueryService
{
    Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);
}
