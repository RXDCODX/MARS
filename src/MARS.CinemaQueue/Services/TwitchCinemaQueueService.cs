using MARS.CinemaQueue.Data;
using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MARS.CinemaQueue.Services;

public interface ITwitchCinemaQueueService
{
    Task<CinemaMediaItemDto?> HandleCinemaQueueRedemptionAsync(
        string userName,
        string userId,
        string userInput,
        string rewardTitle,
        CancellationToken cancellationToken = default
    );
}

public class TwitchCinemaQueueService(
    ICinemaQueueService cinemaQueueService,
    IMediaMetadataService metadataService,
    IDbContextFactory<CinemaDbContext> dbFactory,
    ILogger<TwitchCinemaQueueService> logger
) : ITwitchCinemaQueueService
{
    public async Task<CinemaMediaItemDto?> HandleCinemaQueueRedemptionAsync(
        string userName,
        string userId,
        string userInput,
        string rewardTitle,
        CancellationToken cancellationToken = default
    )
    {
        CinemaMediaItemDto? result = null;

        try
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                logger.LogWarning(
                    "User input is empty for reward redemption by {UserName}",
                    userName
                );
                return result;
            }

            string? validTwitchUserId = null;
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var userExists = await db
                .CinemaQueue.AsNoTracking()
                .AnyAsync(u => u.TwitchUserId == userId, cancellationToken);

            if (userExists)
            {
                validTwitchUserId = userId;
            }
            else
            {
                logger.LogWarning(
                    "Twitch user {UserId} ({UserName}) not found in database, skipping TwitchUserId",
                    userId,
                    userName
                );
            }

            var metadata = await metadataService.GetMetadataAsync(userInput, cancellationToken);

            string title;
            string? description;

            if (metadata != null)
            {
                title = metadata.Title;
                description = metadata.Description;
                logger.LogInformation("Получены метаданные для {Url}: {Title}", userInput, title);
            }
            else
            {
                title = $"Requested by {userName}";
                description = $"Added to queue via reward: {rewardTitle}";
                logger.LogWarning("Не удалось получить метаданные для URL: {Url}", userInput);
            }

            var request = new CreateMediaItemRequest
            {
                Title = title,
                Description = description,
                MediaUrl = userInput,
                TwitchUserId = validTwitchUserId,
                Notes = $"Reward redemption - {DateTime.Now}",
            };

            result = await cinemaQueueService.CreateMediaItemAsync(request, cancellationToken);
            logger.LogInformation("Added media item to queue via reward: {Title}", result.Title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling cinema queue redemption for {UserName}", userName);
        }

        return result;
    }
}
