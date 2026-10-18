using MARS.Telegram.Configuration;
using MARS.Telegram.Models;
using MARS.Telegram.Services.Messaging;
using MARS.Telegram.Services.WTelegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Telegram.Services.BotService;

/// <summary>
/// Бот на MTProto: вход, приём обновлений и пересылка их администраторам.
/// </summary>
/// <remarks>
/// <para>
/// Раньше обновления приходили опросом Bot API (<c>GetUpdates</c>), и вместе с
/// ними ушла возможность работать вовсе: прямой доступ к <c>api.telegram.org</c>
/// закрыт. Теперь сервис подписывается на поток обновлений MTProto и заходит в
/// Telegram через локальный прокси.
/// </para>
/// <para>
/// Экземпляр один на сервис: два клиента одного бота расходятся по сессиям, и
/// обновления приходят каждому. Вход дорогой, поэтому он ленивый — клиент
/// поднимается первым обращением, а не на старте пустого приложения.
/// </para>
/// </remarks>
public sealed class TelegramBotSession(
    IWTelegramClientFactory factory,
    IOptions<TelegramConfiguration> options,
    IServiceProvider serviceProvider,
    ILogger<TelegramBotSession> logger
) : ITelegramBotSession
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private WTelegramClient? _client;
    private ITelegramBotConnection? _connection;

    /// <inheritdoc />
    public async Task<ITelegramBotConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _client is { User: not null })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is not null && _client is { User: not null })
            {
                return _connection;
            }

            var token = options.Value.BotToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "Бот не настроен: задайте Telegram__BotToken (или TELEGRAM_BOT_TOKEN в файле окружения)."
                );
            }

            _client = await factory.LoginAsBotAsync(token, cancellationToken);
            _client.OnUpdates += OnUpdatesAsync;
            _connection = new WTelegramBotConnection(_client);

            logger.LogInformation("Бот авторизован, обновления принимаются");

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Обновления приходят контейнером: внутри — и новое сообщение, и счётчики
    /// <c>pts</c>. Сообщение разбирается здесь же, дальше уходит уже
    /// нормализованным.
    /// </summary>
    private async Task OnUpdatesAsync(TL.UpdatesBase updates)
    {
        foreach (var update in Messages(updates))
        {
            if (update is TL.UpdateNewMessage { message: TL.Message direct })
            {
                await HandleAsync(direct, isChannelPost: false);
            }
            else if (update is TL.UpdateNewChannelMessage { message: TL.Message posted })
            {
                await HandleAsync(posted, isChannelPost: true);
            }
        }
    }

    /// <summary>
    /// Сообщения лежат в двух видах контейнера обновлений: обычном и объединённом
    /// (когда Telegram присылает их пачкой). Контейнер без списка — это
    /// «обновлений слишком много», и брать из него нечего.
    /// </summary>
    private static IEnumerable<TL.Update> Messages(TL.UpdatesBase updates)
    {
        IEnumerable<TL.Update> result = updates switch
        {
            TL.Updates container => container.updates,
            TL.UpdatesCombined combined => combined.updates,
            _ => [],
        };

        return result ?? [];
    }

    private async Task HandleAsync(TL.Message message, bool isChannelPost)
    {
        var normalized = Normalize(message, isChannelPost);

        if (normalized is null)
        {
            return;
        }

        var connection = await GetAsync(CancellationToken.None);

        await ResendToAdminsAsync(connection, normalized);

        // Обработчики берутся из области на каждое сообщение, а не живут в
        // синглтоне: часть из них работает с базой и HTTP, то есть живёт по
        // области. Область создаётся на сообщение — иначе либо сработал бы запрет
        // наScoped-в-синглтон, либо обработчики делили бы одно состояние.
        await using var scope = serviceProvider.CreateAsyncScope();

        foreach (var handler in scope.ServiceProvider.GetServices<ITelegramIncomingHandler>())
        {
            try
            {
                await handler.HandleAsync(connection, normalized, CancellationToken.None);
            }
            catch (Exception e)
            {
                // Один обработчик не должен ронять доставку остальным: обновления
                // MTProto не переспрашиваются, сообщение будет потеряно целиком.
                logger.LogError(e, "Ошибка обработчика входящего сообщения");
            }
        }
    }

    /// <summary>
    /// Пересылка администраторам — то, ради чего бот и существует: администратор
    /// видит всё, что видит бот.
    /// </summary>
    private async Task ResendToAdminsAsync(
        ITelegramBotConnection connection,
        TelegramIncomingMessage message
    )
    {
        var admins = options.Value.AdminIds;

        if (admins.Length == 0)
        {
            logger.LogWarning(
                "Входящее сообщение не переслано: список администраторов пуст (Telegram__AdminIds__0)"
            );
            return;
        }

        foreach (var adminId in admins)
        {
            try
            {
                // Адресат берётся из сообщения, а не строится по числу: чат,
                // группа и канал — это разные InputPeer, и по идентификатору их
                // не различить.
                await connection.ForwardAsync(
                    message.Peer,
                    message.MessageId,
                    new InputPeerUser(adminId, 0)
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обработчика входящего сообщения");
            }
        }
    }

    /// <summary>
    /// Приводит сообщение MTProto к виду, в котором его видят обработчики.
    /// </summary>
    /// <remarks>
    /// Сообщение без текста, медиа и чата обрабатывать нечем: такие приходят
    /// служебными, и нормализованное сообщение было бы пустым.
    /// </remarks>
    public static TelegramIncomingMessage? Normalize(TL.Message message, bool isChannelPost)
    {
        TelegramIncomingMessage? result = null;

        if (InputPeerOf(message.peer_id) is { } peer)
        {
            var (photo, document) = Media(message);

            result = new TelegramIncomingMessage(
                peer.ID,
                peer,
                message.id,
                message.message ?? string.Empty,
                isChannelPost,
                message.grouped_id == 0 ? null : message.grouped_id,
                photo,
                document
            );
        }

        return result;
    }

    /// <summary>
    /// Адресат в виде, в котором его ждёт протокол.
    /// </summary>
    /// <remarks>
    /// По одному идентификатору тип не восстановить: у пользователя, группы и
    /// канала он выглядит одинаково, а <c>InputPeer</c> у них разный. У бота
    /// <c>access_hash</c> не нужен — Telegram допускает нулевой.
    /// </remarks>
    public static InputPeer? InputPeerOf(Peer? peer) =>
        peer switch
        {
            PeerUser user => new InputPeerUser(user.ID, 0),
            PeerChat chat => new InputPeerChat(chat.ID),
            PeerChannel channel => new InputPeerChannel(channel.ID, 0),
            _ => null,
        };

    /// <summary>
    /// Фото и документ лежат в <c>media</c> разными подтипами, а обработчику нужны
    /// конкретные объекты: по ним клиент качает файл.
    /// </summary>
    private static (TL.Photo? Photo, TL.Document? Document) Media(TL.Message message)
    {
        (TL.Photo?, TL.Document?) result = (null, null);

        switch (message.media)
        {
            case TL.MessageMediaPhoto { photo: TL.Photo photo }:
                result = (photo, null);
                break;
            case TL.MessageMediaDocument { document: TL.Document document }:
                result = (null, document);
                break;
        }

        return result;
    }
}
