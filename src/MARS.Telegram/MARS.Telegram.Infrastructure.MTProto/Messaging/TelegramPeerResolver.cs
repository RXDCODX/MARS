using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TL;

namespace MARS.Telegram.Infrastructure.MTProto.Messaging;

/// <summary>
/// Преобразует идентификатор чата (в соглашении Bot API, которое использует
/// конфигурация сервиса) в корректный <see cref="InputPeer"/> для MTProto.
/// </summary>
/// <remarks>
/// <para>
/// В Bot API один числовой идентификатор однозначно определяет чат, а MTProto
/// требует разный <see cref="InputPeer"/> для пользователя, группы и канала.
/// По одному числу тип не восстановить, поэтому используется общепринятое
/// соглашение: положительный — пользователь, отрицательный (но не начинающийся
/// с <c>-100</c>) — старая группа <c>Chat</c>, <c>-100…</c> — супергруппа/канал
/// <c>Channel</c>. Именно так хранит идентификаторы конфигурация
/// (<c>Telegram__LogChatIds__0=-1001803337348</c>).
/// </para>
/// <para>
/// Раньше отправитель оборачивал любой идентификатор в <c>InputPeerUser(id, 0)</c>,
/// и при адресации канала (отрицательный id, несуществующий «пользователь»)
/// Telegram отвечал <c>400 PEER_ID_INVALID</c>.
/// </para>
/// <para>
/// Источники <c>access_hash</c> (в порядке приоритета): кэш в памяти процесса →
/// поставщики хешей (in-memory словари <c>UpdateManager</c>, постоянный реестр
/// пиров в БД) → разрешение канала через <c>Channels_GetChannels</c> → нулевой
/// хеш. Ботам Telegram разрешает нулевой хеш, когда полного хеша нет.
/// </para>
/// </remarks>
public sealed class TelegramPeerResolver(ILogger<TelegramPeerResolver> logger)
{
    private readonly ConcurrentDictionary<long, InputPeer> _cache = new();
    private Func<long, long?>? _userHashProvider;
    private Func<long, long?>? _channelHashProvider;
    private Func<long, Task<long?>>? _asyncUserHashProvider;
    private Func<long, Task<long?>>? _asyncChannelHashProvider;

    /// <summary>
    /// Подключает синхронных поставщиков хешей (in-memory кэш).
    /// </summary>
    public void SetHashProviders(
        Func<long, long?>? userHashProvider,
        Func<long, long?>? channelHashProvider
    )
    {
        _userHashProvider = userHashProvider;
        _channelHashProvider = channelHashProvider;
    }

    /// <summary>
    /// Подключает асинхронных поставщиков хешей (постоянный реестр в БД).
    /// </summary>
    public void SetAsyncHashProviders(
        Func<long, Task<long?>>? asyncUserHashProvider,
        Func<long, Task<long?>>? asyncChannelHashProvider
    )
    {
        _asyncUserHashProvider = asyncUserHashProvider;
        _asyncChannelHashProvider = asyncChannelHashProvider;
    }

    /// <summary>
    /// Кладёт уже разрешённый пир в кэш. Используется коллектором пиров
    /// <c>UpdateManager</c> и постоянным реестром.
    /// </summary>
    public void Cache(long botApiId, InputPeer peer) => _cache[botApiId] = peer;

    /// <summary>
    /// Синхронное преобразование по соглашению идентификаторов без запросов к
    /// Telegram. Возвращает <c>null</c>, если идентификатор не распознан.
    /// Хеш берётся из поставщиков (если есть) или нулевой.
    /// </summary>
    public InputPeer? TryResolve(long id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var (kind, nativeId) = Classify(id);
        if (kind is null)
        {
            return null;
        }

        InputPeer peer = kind switch
        {
            PeerKind.User => new InputPeerUser(nativeId, _userHashProvider?.Invoke(nativeId) ?? 0),
            PeerKind.Chat => new InputPeerChat(nativeId),
            PeerKind.Channel => new InputPeerChannel(
                nativeId,
                _channelHashProvider?.Invoke(nativeId) ?? 0
            ),
            _ => null!,
        };

        if (peer is not null)
        {
            _cache[id] = peer;
        }

        return peer;
    }

    /// <summary>
    /// Полное разрешение: при необходимости обращается к Telegram
    /// (<c>Channels_GetChannels</c> для канала, чтобы получить рабочий хеш).
    /// </summary>
    public async Task<InputPeer?> ResolveAsync(
        long id,
        ITelegramBotConnection connection,
        CancellationToken cancellationToken
    )
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var (kind, nativeId) = Classify(id);
        if (kind is null)
        {
            logger.LogWarning("Не удалось определить тип пира по идентификатору {ChatId}", id);
            return null;
        }

        InputPeer? peer = null;

        switch (kind)
        {
            case PeerKind.User:
                var userHash = _userHashProvider?.Invoke(nativeId) ?? 0;
                if (userHash == 0 && _asyncUserHashProvider is not null)
                {
                    userHash = await _asyncUserHashProvider(nativeId) ?? 0;
                }

                peer = new InputPeerUser(nativeId, userHash);
                break;

            case PeerKind.Chat:
                peer = new InputPeerChat(nativeId);
                break;

            case PeerKind.Channel:
                var hash = _channelHashProvider?.Invoke(nativeId) ?? 0;
                if (hash == 0 && _asyncChannelHashProvider is not null)
                {
                    hash = await _asyncChannelHashProvider(nativeId) ?? 0;
                }

                InputPeerChannel? resolved = null;
                if (hash == 0)
                {
                    resolved = await connection.ResolveChannelAsync(nativeId, cancellationToken);
                    if (resolved is not null)
                    {
                        hash = resolved.access_hash;
                        _cache[id] = resolved;
                    }
                }

                peer =
                    (hash != 0 || resolved is not null)
                        ? new InputPeerChannel(nativeId, hash)
                        : null;
                break;
        }

        if (peer is not null)
        {
            _cache[id] = peer;
        }

        return peer;
    }

    /// <summary>
    /// Разбирает идентификатор по соглашению Bot API. Возвращает
    /// <c>null</c> для нераспознанных значений (например, 0).
    /// </summary>
    public static (PeerKind? Kind, long NativeId) Classify(long id)
    {
        if (id == 0)
        {
            return (null, 0);
        }

        if (id > 0)
        {
            return (PeerKind.User, id);
        }

        // Супергруппа/канал в соглашении Bot API: -100XXXXXXXXXX
        if (id <= -1_000_000_000_000L)
        {
            var nativeId = -id - 1_000_000_000_000L;
            if (nativeId <= 0)
            {
                return (null, 0);
            }

            return (PeerKind.Channel, nativeId);
        }

        // Старая группа Chat: отрицательный, но не -100…
        return (PeerKind.Chat, -id);
    }

    public enum PeerKind
    {
        User,
        Chat,
        Channel,
    }
}
