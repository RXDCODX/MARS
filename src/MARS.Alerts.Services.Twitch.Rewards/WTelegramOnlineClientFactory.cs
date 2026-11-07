using MARS.Alerts.Configuration;
using Microsoft.Extensions.Options;
using TL;
using WTelegramClient = WTelegram.Client;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Настоящий клиент Telegram: авторизуется по номеру телефона из конфигурации и
/// отдаёт наружу только те вызовы, которые нужны сбору мемов.
/// </summary>
public sealed class WTelegramOnlineClientFactory(IOptions<WTelegramConfiguration> config)
    : IOnlineTelegramClientFactory
{
    public async Task<IOnlineTelegramClient> CreateAsync(
        CancellationToken cancellationToken = default
    )
    {
        var settings = config.Value;
        var client = new WTelegramClient(key =>
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

    private sealed class OnlineClient(WTelegramClient client) : IOnlineTelegramClient
    {
        public event Func<UpdatesBase, Task> OnUpdates
        {
            add => client.OnUpdates += value;
            remove => client.OnUpdates -= value;
        }

        public Task<string> DownloadFileAsync(Document document, Stream output) =>
            client.DownloadFileAsync(document, output);

        /// <summary>
        /// Тип файла приходит перечислением, а имя файла собирается строкой, поэтому
        /// наружу отдаётся строковое представление.
        /// </summary>
        public async Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
            (await client.DownloadFileAsync(photo, output, size)).ToString();

        public void Dispose() => client.Dispose();
    }
}
