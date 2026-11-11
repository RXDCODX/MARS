namespace MARS.MediaStorage.Api.Rest.Models;

/// <summary>
/// Ограниченный текстовый preview. Содержимое трактуется как текст,
/// а клиент обязан выводить его через textContent/preformatted text.
/// </summary>
public sealed record MediaStorageTextPreviewDto(
    string Content,
    long SizeBytes,
    bool Truncated
);
