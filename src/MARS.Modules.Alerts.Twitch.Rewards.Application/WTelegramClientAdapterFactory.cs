using System.Diagnostics.CodeAnalysis;
using TL;
using WTelegramClient = WTelegram.Client;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Тонкий адаптер над внешней библиотекой WTelegram. Поведение самой библиотеки
/// проверяется её владельцем; прикладная логика тестируется через адаптер.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "Thin adapter that only forwards calls to the external WTelegram library."
)]
public sealed class WTelegramClientAdapterFactory : IWTelegramClientAdapterFactory
{
    public IWTelegramClientAdapter Create(Func<string, string?> configuration)
    {
        return new Adapter(new WTelegramClient(configuration));
    }

    private sealed class Adapter(WTelegramClient client) : IWTelegramClientAdapter
    {
        public event Func<UpdatesBase, Task> OnUpdates
        {
            add => client.OnUpdates += value;
            remove => client.OnUpdates -= value;
        }

        public Task LoginUserIfNeeded() => client.LoginUserIfNeeded();

        public Task<string> DownloadFileAsync(Document document, Stream output) =>
            client.DownloadFileAsync(document, output);

        public async Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size) =>
            (await client.DownloadFileAsync(photo, output, size)).ToString();

        public void Dispose() => client.Dispose();
    }
}
