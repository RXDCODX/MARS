using MARS.Telegram.Application.Services;
using MARS.Telegram.Domain.Entities;
using MARS.Telegram.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Infrastructure.Postgres.Services;

/// <summary>
/// Постоянный реестр пиров Telegram (id, тип, access_hash, имя). Дополняет
/// in-memory кэш <c>UpdateManager</c>: после рестарта процесса хеши доступны
/// сразу, без повторного обхода диалогов.
/// </summary>
public sealed class TelegramPeerRegistry(
    IDbContextFactory<ChatDbContext> dbContextFactory,
    ILogger<TelegramPeerRegistry> logger
)
{
    public async Task<long?> GetAccessHashAsync(long botApiId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );
            var row = await context
                .Set<TelegramPeer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == botApiId, cancellationToken);

            return row?.AccessHash;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось прочитать access_hash пира {PeerId} из реестра",
                botApiId
            );
            return null;
        }
    }

    public async Task UpsertAsync(
        long botApiId,
        int peerKind,
        long accessHash,
        string? username,
        string? title,
        CancellationToken cancellationToken
    )
    {
        if (accessHash == 0 && peerKind != (int)TelegramPeerKind.Chat)
        {
            // Нулевой хеш для пользователя/канала бесполезен: он и так fallback.
            // Храним только осмысленные (или legacy-группы, у которых хеша нет).
            return;
        }

        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );
            var row = await context
                .Set<TelegramPeer>()
                .FirstOrDefaultAsync(p => p.Id == botApiId, cancellationToken);

            if (row is null)
            {
                context
                    .Set<TelegramPeer>()
                    .Add(
                        new TelegramPeer
                        {
                            Id = botApiId,
                            PeerKind = peerKind,
                            AccessHash = accessHash,
                            Username = username,
                            Title = title,
                            UpdatedAt = DateTimeOffset.UtcNow,
                        }
                    );
            }
            else
            {
                // Не затираем полный хеш мин-хешем: приоритет полного выше.
                if (row.AccessHash == 0 || accessHash != 0)
                {
                    row.AccessHash = accessHash;
                }

                row.PeerKind = peerKind;
                if (!string.IsNullOrWhiteSpace(username))
                {
                    row.Username = username;
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    row.Title = title;
                }

                row.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить пир {PeerId} в реестр", botApiId);
        }
    }
}
