using System.Text.Json;
using MARS.Shared.Models;
using MARS.Telegram.Models;

namespace MARS.Telegram.Services.Booru;

/// <summary>
/// Выборка случайных постов с Rule34 по тегам.
/// </summary>
/// <remarks>
/// Забирается сразу 100 записей, а не <c>limit</c>: DAPI источника отдаёт
/// страницу фиксированного размера, и при <c>limit = 1</c> первый же элемент
/// всегда был бы одним и тем же постом. Из полученной выборки берётся
/// случайное подмножество.
/// </remarks>
public class Rule34RandomPostService(
    ILogger<Rule34RandomPostService> logger,
    IHttpClientFactory factory
) : IRule34RandomPostService
{
    private const string UserAgent = "MarsBot/1.0";
    private const string BaseUrl = "https://api.rule34.xxx/index.php";
    private const int FetchLimit = 100;

    public async Task<OperationResult<IReadOnlyList<Rule34Post>>> GetRandomPostsAsync(
        string tags,
        int limit = 1,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
            "Стартовая ошибка выборки постов"
        );

        if (limit > 0)
        {
            try
            {
                using var httpClient = factory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                var url =
                    $"{BaseUrl}?page=dapi&s=post&q=index&tags={Uri.EscapeDataString(tags)}&limit={FetchLimit}&json=1";

                using var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Rule34 API вернул {StatusCode} для тегов '{Tags}'",
                        response.StatusCode,
                        tags
                    );
                    result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
                        $"Rule34 API вернул код {(int)response.StatusCode}"
                    );
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);

                    result = ReadPosts(content, limit);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при запросе к Rule34 API для тегов '{Tags}'", tags);
                result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
                    $"Ошибка запроса к Rule34 API: {ex.Message}"
                );
            }
        }
        else
        {
            result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
                "Запрошенное число постов должно быть больше нуля"
            );
        }

        return result;
    }

    private static OperationResult<IReadOnlyList<Rule34Post>> ReadPosts(string content, int limit)
    {
        var result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
            "Стартовая ошибка разбора ответа"
        );

        if (!string.IsNullOrWhiteSpace(content))
        {
            Rule34Post[]? posts = null;

            try
            {
                posts = JsonSerializer.Deserialize<Rule34Post[]>(content);
            }
            catch (JsonException)
            {
                posts = null;
            }

            if (posts is null || posts.Length == 0)
            {
                result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
                    "По указанным тегам ничего не найдено"
                );
            }
            else
            {
                // Посты всегда возвращаются по возрастанию id: без сортировки
                // состав выдачи зависел бы от порядка, в котором источник
                // отдал ответ, а при выборке подмножества — ещё и от генератора
                // случайных чисел.
                var picked =
                    posts.Length <= limit
                        ? posts.OrderBy(p => p.Id).ToArray()
                        : Random.Shared.GetItems(posts, limit).OrderBy(p => p.Id).ToArray();

                result = OperationResult<IReadOnlyList<Rule34Post>>.Ok(picked);
            }
        }
        else
        {
            result = OperationResult<IReadOnlyList<Rule34Post>>.Fail(
                "Источник вернул пустой ответ"
            );
        }

        return result;
    }
}
