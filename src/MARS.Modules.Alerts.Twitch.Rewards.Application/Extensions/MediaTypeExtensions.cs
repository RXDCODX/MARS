using MARS.Shared.Contracts.Grpc.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application.Extensions;

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
