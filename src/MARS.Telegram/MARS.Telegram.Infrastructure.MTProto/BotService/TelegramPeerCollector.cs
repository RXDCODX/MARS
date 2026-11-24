using System.Collections.Concurrent;
using MARS.Telegram.Domain.Entities;
using MARS.Telegram.Infrastructure.Postgres.Data;
using MARS.Telegram.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TL;

namespace MARS.Telegram.Infrastructure.MTProto.BotService;

/// <summary>
/// Коллектор пиров для <c>UpdateManager</c>: держит in-memory словари
/// (как стандартный коллектор) и отмечает изменённые записи для выгрузки в
/// постоянный реестр <see cref="TelegramPeer"/> в БД.
/// </summary>
public sealed class TelegramPeerCollector(TelegramPeerRegistry registry, ILogger logger)
    : global::WTelegram.IPeerCollector
{
    private readonly ConcurrentDictionary<long, User> _users = new();
    private readonly ConcurrentDictionary<long, ChatBase> _chats = new();
    private readonly ConcurrentDictionary<long, byte> _dirtyUsers = new();
    private readonly ConcurrentDictionary<long, byte> _dirtyChats = new();

    public IReadOnlyDictionary<long, User> Users => _users;
    public IReadOnlyDictionary<long, ChatBase> Chats => _chats;

    public void Collect(IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            if (user is null)
            {
                continue;
            }

            _users[user.id] = user;

            // Мин-хеш бесполезен для отправки/резолва: сохраняем только полный.
            if (!user.flags.HasFlag(User.Flags.min) && user.access_hash != 0)
            {
                _dirtyUsers.TryAdd(user.id, 0);
            }
        }
    }

    public void Collect(IEnumerable<ChatBase> chats)
    {
        foreach (var chat in chats)
        {
            if (chat is null)
            {
                continue;
            }

            long nativeId = chat switch
            {
                Channel channel => channel.id,
                Chat c => c.id,
                _ => 0,
            };

            if (nativeId == 0)
            {
                continue;
            }

            _chats[nativeId] = chat;

            if (chat is Channel ch && !ch.flags.HasFlag(Channel.Flags.min) && ch.access_hash != 0)
            {
                _dirtyChats.TryAdd(nativeId, 0);
            }
        }
    }

    public bool HasUser(long id) => _users.ContainsKey(id);

    public bool HasChat(long id) => _chats.ContainsKey(id);

    /// <summary>
    /// Выгружает накопленные изменения в БД. Вызывается периодически и при остановке.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        var userIds = _dirtyUsers.Keys.ToArray();
        var chatIds = _dirtyChats.Keys.ToArray();

        foreach (var id in userIds)
        {
            if (!_users.TryGetValue(id, out var user))
            {
                continue;
            }

            try
            {
                await registry.UpsertAsync(
                    botApiId: id,
                    peerKind: (int)TelegramPeerKind.User,
                    accessHash: user.access_hash,
                    username: user.username,
                    title: null,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось выгрузить пира-пользователя {UserId} в реестр",
                    id
                );
            }
        }

        foreach (var nativeId in chatIds)
        {
            if (!_chats.TryGetValue(nativeId, out var chat) || chat is not Channel channel)
            {
                continue;
            }

            var botApiId = -1_000_000_000_000L - nativeId;
            var title = channel.title;

            try
            {
                await registry.UpsertAsync(
                    botApiId: botApiId,
                    peerKind: (int)TelegramPeerKind.Channel,
                    accessHash: channel.access_hash,
                    username: channel.username,
                    title: title,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось выгрузить пир-канал {ChannelId} в реестр",
                    nativeId
                );
            }
        }

        _dirtyUsers.Clear();
        _dirtyChats.Clear();
    }
}
