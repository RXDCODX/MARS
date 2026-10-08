using MARS.Shared.Models;

namespace MARS.Shared.Matoi;

/// <summary>
/// Поиск постов у matoi. Служит и MARS.Alerts (награда RANDOM ART), и
/// MARS.Telegram, поэтому живёт в общей библиотеке.
/// </summary>
public interface IMatoiPostService
{
    /// <summary>
    /// Посты по тегу, безопасные по рейтингу провайдера.
    /// </summary>
    /// <param name="provider">Имя провайдера, например <c>danbooru</c>.</param>
    /// <param name="tags">Теги запроса без рейтинга: он добавляется сам.</param>
    /// <param name="limit">Сколько постов нужно. Реально может прийти меньше.</param>
    /// <param name="cancellationToken">Токен отмены вызывающего.</param>
    Task<OperationResult<IReadOnlyList<MatoiPost>>> GetSafePostsAsync(
        string provider,
        string tags,
        int limit,
        CancellationToken cancellationToken = default
    );
}
