using MARS.Telegram.Application.Services;
using MARS.Telegram.Domain.Entities;
using MARS.Telegram.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Infrastructure.Postgres.Services;

/// <summary>
/// Хранилище меток «прочитано» по чатам в PostgreSQL.
/// </summary>
public sealed class TelegramChatReadStateStore(
    IDbContextFactory<ChatDbContext> dbContextFactory,
    ILogger<TelegramChatReadStateStore> logger
) : ITelegramChatReadStateStore
{
    public async Task<IReadOnlyDictionary<long, int>> LoadAllAsync(
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<long, int>();

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var rows = await dbContext
                .TelegramChatReadStates.AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                result[row.ChatId] = row.LastReadMessageId;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить метки прочитанных сообщений Telegram");
        }

        return result;
    }

    public async Task MarkReadAsync(
        long chatId,
        int peerKind,
        int messageId,
        DateTimeOffset at,
        CancellationToken cancellationToken
    )
    {
        if (messageId <= 0)
        {
            return;
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var existing = await dbContext.TelegramChatReadStates.FirstOrDefaultAsync(
                r => r.ChatId == chatId,
                cancellationToken
            );

            if (existing is null)
            {
                dbContext.TelegramChatReadStates.Add(
                    new TelegramChatReadState
                    {
                        ChatId = chatId,
                        PeerKind = peerKind,
                        LastReadMessageId = messageId,
                        LastReadAt = at,
                    }
                );
            }
            else
            {
                // Значение только растёт: повторная доставка старого сообщения
                // не откатывает метку назад.
                if (messageId > existing.LastReadMessageId)
                {
                    existing.LastReadMessageId = messageId;
                    existing.PeerKind = peerKind;
                    existing.LastReadAt = at;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Не удалось сохранить метку прочитанного сообщения {MessageId} в чате {ChatId}",
                messageId,
                chatId
            );
            throw;
        }
    }
}
