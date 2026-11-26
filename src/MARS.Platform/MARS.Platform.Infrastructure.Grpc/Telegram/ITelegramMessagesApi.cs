namespace MARS.Platform.Infrastructure.Grpc.Telegram;

public interface ITelegramMessagesApi
{
    Task<bool> SendAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
