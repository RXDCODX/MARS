namespace MARS.Telegram.Infrastructure.Integrations.Telegram.PrivateChannelsResender;

/// <summary>
/// Внутренний порт инфраструктурного фонового обработчика для получения MTProto-клиента.
/// Не входит в публичный Application-контракт управления авторизацией.
/// </summary>
public interface IWTelegramChannelClientProvider
{
    Task<IWTelegramChannelClient> GetClientAsync(CancellationToken cancellationToken = default);
}
