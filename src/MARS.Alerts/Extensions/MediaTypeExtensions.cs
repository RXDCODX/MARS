using MARS.Shared.Hubs.Models;
using MARS.Shared.Models.Media;

namespace MARS.Alerts.Extensions;

public static class MediaTypeExtensions
{
    public static ValueTask<MediaType> GetFileMediaTypeAsync(this string? text)
    {
        return ValueTask.FromResult(text.GetFileMediaType());
    }

    public static MediaType GetFileMediaType(this string? text)
    {
        var extension = text?.ToLower();

        return extension switch
        {
            ".tgs" => MediaType.TelegramSticker,
            ".ogg" or ".oga" => MediaType.Audio,
            ".webm" or ".mp4" => MediaType.Video,
            ".jpg" or ".jpeg" or ".png" or ".webp" => MediaType.Image,
            ".gif" => MediaType.Gif,
            ".mp3" or ".wav" => MediaType.Audio,
            _ => MediaType.None,
        };
    }
}
