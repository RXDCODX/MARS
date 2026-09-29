using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Extensions;

public static class StringExtension
{
    extension(string? text)
    {
        public ValueTask<MediaType> GetFileMediaTypeAsync()
        {
            return ValueTask.FromResult(text.GetFileMediaType());
        }

        public MediaType GetFileMediaType()
        {
            var exstension = text?.ToLower();

            return exstension switch
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
}
