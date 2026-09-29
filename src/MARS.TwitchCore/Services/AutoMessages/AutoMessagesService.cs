using MARS.TwitchCore.Data;
using MARS.TwitchCore.DTOs;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.TwitchCore.Services.AutoMessages;

public interface IAutoMessagesService
{
    Task<IEnumerable<AutoMessageDto>> GetAllAutoMessagesAsync(
        CancellationToken cancellationToken = default
    );
    Task<AutoMessageDto?> GetAutoMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
    Task<AutoMessageDto> CreateAutoMessageAsync(
        CreateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    );
    Task<AutoMessageDto?> UpdateAutoMessageAsync(
        Guid id,
        UpdateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteAutoMessageAsync(Guid id, CancellationToken cancellationToken = default);
}

public class AutoMessagesService(
    IDbContextFactory<TwitchDbContext> dbContextFactory,
    ILogger<AutoMessagesService> logger
) : IAutoMessagesService
{
    public async Task<IEnumerable<AutoMessageDto>> GetAllAutoMessagesAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var messages = await db
            .AutoMessages.AsNoTracking()
            .OrderBy(m => m.Message)
            .ToListAsync(cancellationToken);

        return messages.Select(m => new AutoMessageDto { Id = m.Id, Message = m.Message });
    }

    public async Task<AutoMessageDto?> GetAutoMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var message = await db
            .AutoMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return message is null
            ? null
            : new AutoMessageDto { Id = message.Id, Message = message.Message };
    }

    public async Task<AutoMessageDto> CreateAutoMessageAsync(
        CreateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = new AutoMessage { Message = request.Message };
        db.AutoMessages.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Создано автоматическое сообщение с ID: {Id}", entity.Id);

        return new AutoMessageDto { Id = entity.Id, Message = entity.Message };
    }

    public async Task<AutoMessageDto?> UpdateAutoMessageAsync(
        Guid id,
        UpdateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.AutoMessages.FirstOrDefaultAsync(
            m => m.Id == id,
            cancellationToken
        );

        if (entity is null)
        {
            return null;
        }

        entity.Message = request.Message ?? entity.Message;
        db.AutoMessages.Update(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new AutoMessageDto { Id = entity.Id, Message = entity.Message };
    }

    public async Task<bool> DeleteAutoMessageAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.AutoMessages.FirstOrDefaultAsync(
            m => m.Id == id,
            cancellationToken
        );

        if (entity is null)
        {
            return false;
        }

        db.AutoMessages.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Удалено автоматическое сообщение с ID: {Id}", id);
        return true;
    }
}
