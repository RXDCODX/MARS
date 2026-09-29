using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MARS.CinemaQueue.Services;

public interface IMediaMetadataService
{
    Task<MediaMetadata?> GetMetadataAsync(
        string url,
        CancellationToken cancellationToken = default
    );
}

public class MediaMetadata
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
}

public class MediaMetadataService(
    IKinopoiskService kinopoiskService,
    ILogger<MediaMetadataService> logger
) : IMediaMetadataService
{
    private static readonly Regex KinopoiskUrlRegex = new(
        @"https://www\.kinopoisk\.ru/film/(\d+)",
        RegexOptions.Compiled
    );

    public async Task<MediaMetadata?> GetMetadataAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        MediaMetadata? result = null;

        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                if (KinopoiskUrlRegex.IsMatch(url))
                {
                    result = await GetKinopoiskMetadataAsync(url, cancellationToken);
                }
                else
                {
                    logger.LogWarning("Неподдерживаемый домен для URL: {Url}", url);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении метаданных для URL: {Url}", url);
            }
        }

        return result;
    }

    private async Task<MediaMetadata?> GetKinopoiskMetadataAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        MediaMetadata? result = null;

        try
        {
            var movie = await kinopoiskService.GetMovieByUrlAsync(url, cancellationToken);
            if (movie != null)
            {
                var title = movie.Name;
                if (!string.IsNullOrWhiteSpace(title) && movie.Year.HasValue)
                {
                    title = $"{title} ({movie.Year})";
                }

                var description = movie.Description ?? movie.ShortDescription;
                var imageUrl = movie.Poster?.Url;

                if (!string.IsNullOrWhiteSpace(title))
                {
                    result = new MediaMetadata
                    {
                        Title = title,
                        Description = description,
                        ImageUrl = imageUrl,
                        SourceUrl = url,
                    };
                }
                else
                {
                    logger.LogWarning(
                        "Не удалось получить title для фильма из Кинопоиска: {Url}",
                        url
                    );
                }
            }
            else
            {
                logger.LogWarning("Фильм не найден в Кинопоиске для URL: {Url}", url);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении метаданных Кинопоиска для URL: {Url}", url);
        }

        return result;
    }
}
