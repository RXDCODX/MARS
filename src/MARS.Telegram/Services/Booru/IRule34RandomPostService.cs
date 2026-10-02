using MARS.Shared.Models;
using MARS.Telegram.Models;

namespace MARS.Telegram.Services.Booru;

/// <summary>
/// Выборка постов с booru-источника.
/// </summary>
public interface IRule34RandomPostService
{
    Task<OperationResult<IReadOnlyList<Rule34Post>>> GetRandomPostsAsync(
        string tags,
        int limit = 1,
        CancellationToken cancellationToken = default
    );
}
