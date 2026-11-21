using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Alerts.Application.TwitchRewards.Extensions;

/// <summary>
/// Оболочка над общей таблицей расширений: сохраняет локальный API наград.
/// </summary>
public static class MediaTypeExtensions
{
    public static ValueTask<MediaType> GetFileMediaTypeAsync(this string? text) =>
        ValueTask.FromResult(text.GetFileMediaType());

    public static MediaType GetFileMediaType(this string? text) =>
        MediaFileTypes.GetMediaType(text);
}
