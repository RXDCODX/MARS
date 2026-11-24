using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Domain.Entities;
using MARS.Telegram.Infrastructure.MTProto.Messaging;
using MARS.Telegram.Infrastructure.MTProto.Models;
using MARS.Telegram.Infrastructure.MTProto.WTelegram;
using MARS.Telegram.Infrastructure.Postgres.Data;
using MARS.Telegram.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TL;

namespace MARS.Telegram.Infrastructure.MTProto.BotService;

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
    IDbContextFactory<ChatDbContext> dbContextFactory,
    TelegramPeerResolver peerResolver,
    TelegramPeerRegistry peerRegistry,
    ITelegramChatReadStateStore readStateStore,
    ILogger<TelegramBotSession> logger
) : ITelegramBotSession, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IDbContextFactory<ChatDbContext> _dbContextFactory = dbContextFactory;
    private readonly TelegramPeerRegistry _peerRegistry = peerRegistry;
    private readonly ITelegramChatReadStateStore _readStateStore = readStateStore;
    private TelegramPeerCollector? _peerCollector;
    private WTelegramClient? _client;
    private global::WTelegram.UpdateManager? _updateManager;
    private ITelegramBotConnection? _connection;
    private TelegramBotSessionSnapshot _snapshot = new();
    private IReadOnlyDictionary<long, int> _readState = new Dictionary<long, int>();
    private readonly Lock _stateSaveLock = new();
    private DateTimeOffset _lastStateSave = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public TelegramBotSessionSnapshot? Snapshot => _snapshot;

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
                var error =
                    "Бот не настроен: задайте Telegram__BotToken (или TELEGRAM_BOT_TOKEN в файле окружения).";
                _snapshot = new TelegramBotSessionSnapshot
                {
                    IsConnected = false,
                    ErrorMessage = error,
                };
                throw new InvalidOperationException(error);
            }

            try
            {
                // Метки «прочитано» и состояние потока обновлений грузим до входа:
                // UpdateManager нужно передать состояние при подключении, а
                // поставщики хешей — до первого обновления.
                _readState = await _readStateStore.LoadAllAsync(cancellationToken);
                var updatesState = await LoadUpdatesStateAsync(cancellationToken);

                _client = await factory.LoginAsBotAsync(
                    token,
                    cancellationToken,
                    client =>
                    {
                        _peerCollector = new TelegramPeerCollector(_peerRegistry, logger);

                        _updateManager = client.WithUpdateManager(
                            OnUpdateAsync,
                            updatesState,
                            _peerCollector,
                            reentrant: false
                        );

                        peerResolver.SetHashProviders(
                            id => ResolveUserHash(id),
                            id => ResolveChannelHash(id)
                        );

                        peerResolver.SetAsyncHashProviders(
                            id => _peerRegistry.GetAccessHashAsync(id, CancellationToken.None),
                            nativeId =>
                            {
                                var botApiId = -1_000_000_000_000L - nativeId;
                                return _peerRegistry.GetAccessHashAsync(
                                    botApiId,
                                    CancellationToken.None
                                );
                            }
                        );
                    }
                );

                _connection = new WTelegramBotConnection(_client);

                var user = _client.User;
                _snapshot = new TelegramBotSessionSnapshot
                {
                    IsConnected = user is not null,
                    BotUserId = user?.ID,
                    BotUsername = user?.ActiveUsernames is null
                        ? null
                        : string.Join(", ", user.ActiveUsernames),
                    ErrorMessage = null,
                };

                logger.LogInformation("Бот авторизован, обновления принимаются");

                return _connection;
            }
            catch (Exception e)
            {
                _snapshot = new TelegramBotSessionSnapshot
                {
                    IsConnected = false,
                    ErrorMessage = e.Message,
                };
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Обработчик одного обновления от <c>UpdateManager</c>. Менеджер уже
    /// обеспечил правильный порядок и дедупликацию по <c>pts</c>; здесь
    /// остаётся разобрать сообщение, отфильтровать уже прочитанные (per-chat
    /// метка) и прогнать через обработчики.
    /// </summary>
    private async Task OnUpdateAsync(TL.Update update)
    {
        TL.Message? message = null;
        var isChannelPost = false;

        if (update is TL.UpdateNewMessage { message: TL.Message direct })
        {
            message = direct;
            isChannelPost = false;
        }
        else if (update is TL.UpdateNewChannelMessage { message: TL.Message posted })
        {
            message = posted;
            isChannelPost = true;
        }

        if (message is null)
        {
            return;
        }

        await HandleAsync(message, isChannelPost);
    }

    private async Task HandleAsync(TL.Message message, bool isChannelPost)
    {
        var normalized = Normalize(message, isChannelPost);

        if (normalized is null)
        {
            return;
        }

        // Дедуп: если это сообщение (или более позднее) уже обработано —
        // пропускаем. Защищает от повторного выполнения команд при replay
        // пропущенных обновлений после сбоя.
        if (
            _readState.TryGetValue(normalized.ChatId, out var lastRead)
            && normalized.MessageId <= lastRead
        )
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

        // Метка ставится после прогона всех обработчиков: если процесс упадёт
        // раньше, сообщение будет обработано повторно (at-least-once), но не потеряно.
        var peerKind = normalized.Peer switch
        {
            TL.InputPeerUser => 0,
            TL.InputPeerChat => 1,
            TL.InputPeerChannel => 2,
            _ => 0,
        };

        try
        {
            await _readStateStore.MarkReadAsync(
                normalized.ChatId,
                peerKind,
                normalized.MessageId,
                DateTimeOffset.UtcNow,
                CancellationToken.None
            );
            var updated = new Dictionary<long, int>(_readState)
            {
                [normalized.ChatId] = Math.Max(
                    _readState.TryGetValue(normalized.ChatId, out var prev) ? prev : 0,
                    normalized.MessageId
                ),
            };
            _readState = updated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Не удалось сохранить метку прочитанного сообщения");
        }

        // Дебаунс-сохранение состояния потока обновлений.
        await MaybeSaveUpdatesStateAsync();
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
                var adminPeer = await peerResolver.ResolveAsync(
                    adminId,
                    connection,
                    CancellationToken.None
                );
                if (adminPeer is null)
                {
                    logger.LogError(
                        "Входящее сообщение не переслано администратору {AdminId}: не удалось определить адресат",
                        adminId
                    );
                    continue;
                }

                await connection.ForwardAsync(message.Peer, message.MessageId, adminPeer);
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

    private async Task<Dictionary<
        long,
        global::WTelegram.UpdateManager.MBoxState
    >?> LoadUpdatesStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );
            var row = await context
                .RootState.AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Name == RootStateKeys.WTelegramBotUpdatesState,
                    cancellationToken
                );

            if (row is null || string.IsNullOrWhiteSpace(row.Value))
            {
                return null;
            }

            return System.Text.Json.JsonSerializer.Deserialize<
                Dictionary<long, global::WTelegram.UpdateManager.MBoxState>
            >(row.Value);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Не удалось загрузить состояние потока обновлений Telegram");
            return null;
        }
    }

    private long? ResolveUserHash(long userId)
    {
        if (_peerCollector is null)
        {
            return null;
        }

        if (
            _peerCollector.Users.TryGetValue(userId, out var user)
            && !user.flags.HasFlag(TL.User.Flags.min)
            && user.access_hash != 0
        )
        {
            return user.access_hash;
        }

        return null;
    }

    private long? ResolveChannelHash(long nativeChannelId)
    {
        if (_peerCollector is null)
        {
            return null;
        }

        if (
            _peerCollector.Chats.TryGetValue(nativeChannelId, out var chat)
            && chat is TL.Channel channel
            && !channel.flags.HasFlag(TL.Channel.Flags.min)
            && channel.access_hash != 0
        )
        {
            return channel.access_hash;
        }

        return null;
    }

    private async Task MaybeSaveUpdatesStateAsync()
    {
        if (_updateManager is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - _lastStateSave < TimeSpan.FromSeconds(30))
        {
            return;
        }

        await SaveUpdatesStateAsync(CancellationToken.None);

        if (_peerCollector is not null)
        {
            try
            {
                await _peerCollector.FlushAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Не удалось выгрузить реестр пиров Telegram");
            }
        }
    }

    private async Task SaveUpdatesStateAsync(CancellationToken cancellationToken)
    {
        if (_updateManager is null)
        {
            return;
        }

        lock (_stateSaveLock)
        {
            if (DateTimeOffset.UtcNow - _lastStateSave < TimeSpan.FromSeconds(5))
            {
                return;
            }
        }

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_updateManager.State);
            await using var context = await _dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );
            var row = await context.RootState.FirstOrDefaultAsync(
                r => r.Name == RootStateKeys.WTelegramBotUpdatesState,
                cancellationToken
            );

            if (row is null)
            {
                context.RootState.Add(
                    new RootState
                    {
                        Name = RootStateKeys.WTelegramBotUpdatesState,
                        Value = json,
                        Description = "Состояние потока обновлений MTProto бота (pts/qts/seq).",
                        TypeDescription = "JSON Dictionary<long, MBoxState>",
                    }
                );
            }
            else
            {
                row.Value = json;
            }

            await context.SaveChangesAsync(cancellationToken);
            _lastStateSave = DateTimeOffset.UtcNow;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Не удалось сохранить состояние потока обновлений Telegram");
        }
    }

    public void Dispose()
    {
        try
        {
            SaveUpdatesStateAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            // best effort on shutdown
        }

        if (_peerCollector is not null)
        {
            try
            {
                _peerCollector.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch
            {
                // best effort on shutdown
            }
        }

        _client?.Dispose();
    }

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
