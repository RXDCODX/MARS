using MARS.Shared.Models;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Services;

public class TelegramDiscordBridgeService(
    IDbContextFactory<ChatDbContext> dbFactory,
    ILogger<TelegramDiscordBridgeService> logger
) : ITelegramDiscordBridgeService
{
    public async Task<OperationResult<List<TelegramDiscordBindingDto>>> GetBindingsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<List<TelegramDiscordBindingDto>>.Fail("Ошибка получения связей");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var bindings = await db
                .TelegramDiscordChannelBindings.AsNoTracking()
                .Select(e => new TelegramDiscordBindingDto
                {
                    Id = e.Id,
                    TelegramChannelId = e.TelegramChannelId,
                    DiscordChannelId = e.DiscordChannelId,
                    IsEnabled = e.IsEnabled,
                    CreatedAtUtc = e.CreatedAtUtc,
                    UpdatedAtUtc = e.UpdatedAtUtc,
                })
                .ToListAsync(cancellationToken);

            result = OperationResult<List<TelegramDiscordBindingDto>>.Ok(bindings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения Telegram-Discord связей");
        }

        return result;
    }

    public async Task<OperationResult<TelegramDiscordBindingDto>> AddBindingAsync(
        TelegramDiscordBindingCreateRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<TelegramDiscordBindingDto>.Fail("Ошибка добавления связи");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var entity = new TelegramDiscordChannelBinding
            {
                Id = Guid.CreateVersion7(DateTime.Now),
                TelegramChannelId = request.TelegramChannelId,
                DiscordChannelId = request.DiscordChannelId,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            db.TelegramDiscordChannelBindings.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            result = OperationResult<TelegramDiscordBindingDto>.Ok(
                new TelegramDiscordBindingDto
                {
                    Id = entity.Id,
                    TelegramChannelId = entity.TelegramChannelId,
                    DiscordChannelId = entity.DiscordChannelId,
                    IsEnabled = entity.IsEnabled,
                    CreatedAtUtc = entity.CreatedAtUtc,
                    UpdatedAtUtc = entity.UpdatedAtUtc,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка добавления Telegram-Discord связи");
        }

        return result;
    }

    public async Task<OperationResult> DeleteBindingAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult.Fail("Ошибка удаления связи");

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var entity = await db.TelegramDiscordChannelBindings.FindAsync(
                [id],
                cancellationToken
            );

            if (entity is not null)
            {
                db.TelegramDiscordChannelBindings.Remove(entity);
                await db.SaveChangesAsync(cancellationToken);
                result = OperationResult.Ok();
            }
            else
            {
                result = OperationResult.Fail("Связь не найдена");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка удаления Telegram-Discord связи {BindingId}", id);
        }

        return result;
    }

    public async Task<OperationResult<TelegramDiscordBindingDto>> SetBindingEnabledAsync(
        Guid id,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<TelegramDiscordBindingDto>.Fail(
            "Ошибка обновления состояния связи"
        );

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var entity = await db.TelegramDiscordChannelBindings.FindAsync(
                [id],
                cancellationToken
            );

            if (entity is not null)
            {
                entity.IsEnabled = isEnabled;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                result = OperationResult<TelegramDiscordBindingDto>.Ok(
                    new TelegramDiscordBindingDto
                    {
                        Id = entity.Id,
                        TelegramChannelId = entity.TelegramChannelId,
                        DiscordChannelId = entity.DiscordChannelId,
                        IsEnabled = entity.IsEnabled,
                        CreatedAtUtc = entity.CreatedAtUtc,
                        UpdatedAtUtc = entity.UpdatedAtUtc,
                    }
                );
            }
            else
            {
                result = OperationResult<TelegramDiscordBindingDto>.Fail("Связь не найдена");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка обновления состояния Telegram-Discord связи {BindingId}",
                id
            );
        }

        return result;
    }

    public async Task<OperationResult<List<TelegramDiscordChannelStateDto>>> GetStatesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<List<TelegramDiscordChannelStateDto>>.Fail(
            "Ошибка чтения состояния"
        );

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var states = await db
                .TelegramDiscordChannelStates.AsNoTracking()
                .Select(e => new TelegramDiscordChannelStateDto
                {
                    TelegramChannelId = e.TelegramChannelId,
                    LastProcessedMessageId = e.LastProcessedMessageId,
                    LastUpdatedUtc = e.LastUpdatedUtc,
                })
                .ToListAsync(cancellationToken);

            result = OperationResult<List<TelegramDiscordChannelStateDto>>.Ok(states);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка чтения состояния Telegram-Discord bridge");
        }

        return result;
    }

    public Task<OperationResult<List<TelegramChannelOptionDto>>> GetTelegramChannelsAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Requires WTelegram client to list channels — returns empty in standalone microservice
        return Task.FromResult(
            OperationResult<List<TelegramChannelOptionDto>>.Ok([])
        );
    }

    public Task<OperationResult<List<DiscordChannelOptionDto>>> GetDiscordChannelsAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Requires Discord client to list channels — returns empty in standalone microservice
        return Task.FromResult(
            OperationResult<List<DiscordChannelOptionDto>>.Ok([])
        );
    }
}
