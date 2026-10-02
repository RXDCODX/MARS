using MARS.Shared.Models;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Services.Booru;

/// <summary>
/// Дедупликация публикаций по тройке «источник + id изображения + канал».
/// </summary>
/// <remarks>
/// Отличие от монолита — результат возвращается, а не проглатывается внутри
/// <c>catch</c>: ошибка записи отметки означает риск повторной публикации, и
/// планировщик должен знать об этом, а не публиковать изображение дважды.
/// </remarks>
public sealed class DeduplicationService(
    IDbContextFactory<ChatDbContext> dbContextFactory,
    ILogger<DeduplicationService> logger
) : IDeduplicationService
{
    public async Task<OperationResult<bool>> IsAlreadyPostedAsync(
        string source,
        int imageId,
        ulong discordChannelId,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<bool>.Fail("Стартовая ошибка проверки дубликата");

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var already = await dbContext
                .PostedImageRecords.AsNoTracking()
                .AnyAsync(
                    r =>
                        r.Source == source
                        && r.ImageId == imageId
                        && r.DiscordChannelId == discordChannelId,
                    cancellationToken
                );

            result = OperationResult<bool>.Ok(already);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка проверки дубликата изображения {Source}:{ImageId} в канале {ChannelId}",
                source,
                imageId,
                discordChannelId
            );
            result = OperationResult<bool>.Fail($"Ошибка проверки дубликата: {ex.Message}");
        }

        return result;
    }

    public async Task<OperationResult> RecordPostAsync(
        string source,
        int imageId,
        ulong discordChannelId,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult.Fail("Стартовая ошибка записи отметки о публикации");

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            dbContext.PostedImageRecords.Add(
                new PostedImageRecord
                {
                    Source = source,
                    ImageId = imageId,
                    DiscordChannelId = discordChannelId,
                    PostedAtUtc = DateTime.UtcNow,
                }
            );

            await dbContext.SaveChangesAsync(cancellationToken);

            result = OperationResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка записи отметки о публикации {Source}:{ImageId} в канале {ChannelId}",
                source,
                imageId,
                discordChannelId
            );
            result = OperationResult.Fail($"Ошибка записи отметки о публикации: {ex.Message}");
        }

        return result;
    }
}
