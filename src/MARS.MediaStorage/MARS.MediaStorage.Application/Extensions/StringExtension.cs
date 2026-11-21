using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Application.Extensions;

/// <summary>
/// Оболочка над общей таблицей расширений: сохраняет локальный API хранилища.
/// </summary>
public static class StringExtension
{
    extension(string? text)
    {
        public ValueTask<MediaType> GetFileMediaTypeAsync() =>
            ValueTask.FromResult(text.GetFileMediaType());

        public MediaType GetFileMediaType() => MediaFileTypes.GetMediaType(text);
    }
}
