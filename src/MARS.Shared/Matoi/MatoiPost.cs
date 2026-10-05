using System.Text.Json.Serialization;

namespace MARS.Shared.Matoi;

/// <summary>
/// Один пост в ответе matoi. Поля названы по фактическим ключам ответа.
/// </summary>
/// <remarks>
/// Приводится только то, чем реально пользуется оверлей. Полный список полей
/// booru — два десятка свойств, и тащить их в общую библиотеку незачем: лишние
/// поля не читаются, а проверка публичного контракта проходит по каждому из
/// них отдельно.
/// </remarks>
public sealed class MatoiPost
{
    /// <summary>Идентификатор поста у источника. Может совпадать у разных провайдеров.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Прямая ссылка на файл. Оверлей получает её как есть, без прокси.
    /// </summary>
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }

    /// <summary>Ссылка на превью — запасной вариант, если основной файла нет.</summary>
    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; set; }

    /// <summary>Ссылка на уменьшенную копию.</summary>
    [JsonPropertyName("sample_url")]
    public string? SampleUrl { get; set; }

    /// <summary>
    /// Рейтинг источника: <c>g</c>, <c>s</c>, <c>q</c>, <c>e</c> — и не одно и
    /// то же у разных провайдеров.
    /// </summary>
    /// <remarks>
    /// Проверять безопасность обязательно по этому полю, а не по тегам: matoi
    /// вырезает запрошенный тег из ответа, поэтому в <see cref="Tags"/> запрошенного
    /// рейтинга нет, а признать пост чувствительным по тегам не выйдет.
    /// </remarks>
    [JsonPropertyName("rating")]
    public string? Rating { get; set; }

    /// <summary>Оценка поста у источника.</summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>Теги поста.</summary>
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    /// <summary>Ссылка на страницу поста у источника.</summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }
}
