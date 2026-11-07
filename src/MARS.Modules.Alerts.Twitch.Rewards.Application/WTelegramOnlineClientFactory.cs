using MARS.Modules.Alerts.Twitch.Rewards.Application.Configuration;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Настоящий клиент Telegram: авторизуется по номеру телефона из конфигурации и
/// отдаёт наружу только те вызовы, которые нужны сбору мемов.
/// </summary>
public sealed class WTelegramOnlineClientFactory(
    IOptions<WTelegramConfiguration> config,
    IWTelegramClientAdapterFactory clientFactory
) : IOnlineTelegramClientFactory
{
    public async Task<IOnlineTelegramClient> CreateAsync(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = config.Value;
        var client = clientFactory.Create(key =>
            key switch
            {
                "api_id" => settings.AppId.ToString(),
                "api_hash" => settings.ApiHash,
                "phone_number" => settings.PhoneNumber,
                "password" => settings.Password,
                _ => null,
            }
        );

        await client.LoginUserIfNeeded();

        return new OnlineClient(client);
    }

    private sealed class OnlineClient(IWTelegramClientAdapter client) : IOnlineTelegramClient
    {
        public event Func<UpdatesBase, Task> OnUpdates
        {
            add => client.OnUpdates += value;
            remove => client.OnUpdates -= value;
        }

        public Task<string> DownloadFileAsync(Document document, Stream output) =>
            client.DownloadFileAsync(document, output);

        public Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
            client.DownloadFileAsync(photo, output, size);

        public void Dispose() => client.Dispose();
    }
}
