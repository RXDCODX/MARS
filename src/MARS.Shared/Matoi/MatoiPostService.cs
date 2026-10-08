using System.Net;
using System.Text.Json;
using MARS.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Matoi;

/// <summary>
/// Клиент matoi по его HTTP API.
/// </summary>
/// <remarks>
/// Поведение сверено с живым стендом, а не с документацией, и три его особенности
/// определяют всю реализацию:
/// <list type="number">
/// <item>маршрут провайдерный — <c>/api/{провайдер}/posts</c>, а не общий
/// <c>/api/posts</c>;</item>
/// <item>рейтинг у провайдеров разный, поэтому безопасный тег берётся из
/// <see cref="MatoiProviderCatalog"/>, а значение рейтинга проверяется ещё раз на
/// клиенте — тег в запросе это первая линия, поле в ответе вторая;</item>
/// <item>matoi не дополняет выдачу до <c>limit</c> и не сообщает об этом: вместо
/// этого выбирается случайная страница, а нехватка добирается со следующих.</item>
/// </list>
/// </remarks>
public class MatoiPostService(
    HttpClient httpClient,
    IOptions<MatoiOptions> options,
    ILogger<MatoiPostService> logger
) : IMatoiPostService
{
    /// <summary>Ключ сериализации: имена свойств у matoi в snake_case.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<OperationResult<IReadOnlyList<MatoiPost>>> GetSafePostsAsync(
        string provider,
        string tags,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        OperationResult<IReadOnlyList<MatoiPost>> result = OperationResult<
            IReadOnlyList<MatoiPost>
        >.Fail("Стартовая ошибка: запрос не выполнен");

        var settings = options.Value;
        var wanted = Math.Clamp(limit, 1, Math.Max(1, settings.MaxLimit));

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            result = OperationResult<IReadOnlyList<MatoiPost>>.Fail(
                "Не задан Matoi:BaseUrl — клиент matoi не настроен"
            );
        }
        else if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            // Не идём по сети: пустой ключ matoi трактует как «авторизация не
            // включена», и /api/* становится открытым всему, что дотянется до
            // контейнера. Тихая отправка запроса была бы отправкой без ключа.
            result = OperationResult<IReadOnlyList<MatoiPost>>.Fail(
                "Не задан Matoi:ApiKey — matoi без ключа не включает авторизацию",
                HttpStatusCode.Unauthorized
            );
        }
        else if (string.IsNullOrWhiteSpace(tags))
        {
            result = OperationResult<IReadOnlyList<MatoiPost>>.Fail("Пустой тег запроса");
        }
        else if (!MatoiProviderCatalog.TryGet(provider, out var rating))
        {
            // Не угадываем словарь рейтингов: цена ошибки — чувствительное в
            // оверлее. Известные провайдеры перечислены, чтобы ошибка была
            // исправимой по тексту, а не загадочной.
            result = OperationResult<IReadOnlyList<MatoiPost>>.Fail(
                $"Провайдер \"{provider}\" не описан: нет проверенного словаря"
                    + $" рейтингов. Известны: {string.Join(", ", MatoiProviderCatalog.KnownProviders)}",
                HttpStatusCode.BadRequest
            );
        }
        else
        {
            result = await CollectAsync(
                provider,
                tags,
                rating,
                wanted,
                settings,
                cancellationToken
            );
        }

        return result;
    }

    private async Task<OperationResult<IReadOnlyList<MatoiPost>>> CollectAsync(
        string provider,
        string tags,
        MatoiRating rating,
        int limit,
        MatoiOptions settings,
        CancellationToken cancellationToken
    )
    {
        var collected = new List<MatoiPost>(limit);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        OperationResult<IReadOnlyList<MatoiPost>> result;

        var queryTags = $"{tags} {rating.SafeRequestTag}".Trim();
        var page = Random.Shared.Next(1, Math.Max(1, settings.MaxPage) + 1);
        var attempts = Math.Max(1, settings.PageAttempts);
        var transportError = string.Empty;
        var lastStatus = HttpStatusCode.OK;

        for (var attempt = 0; attempt < attempts && collected.Count < limit; attempt++)
        {
            var pageResult = await ReadPageAsync(
                provider,
                queryTags,
                limit,
                page + attempt,
                cancellationToken
            );

            if (pageResult.Posts is not null)
            {
                AppendSafe(collected, seen, pageResult.Posts, rating, limit);
            }
            else
            {
                transportError = pageResult.Error ?? transportError;
                lastStatus = pageResult.Status;
            }
        }

        if (collected.Count > 0)
        {
            // Часть постов набралась даже при неполных страницах — это полезный
            // результат, а не ошибка: matoi режет выдачу всегда.
            result = OperationResult<IReadOnlyList<MatoiPost>>.Ok(collected);
        }
        else if (!string.IsNullOrWhiteSpace(transportError))
        {
            logger.LogWarning(
                "Matoi {Provider}: {Error} (tags={Tags}, limit={Limit})",
                provider,
                transportError,
                tags,
                limit
            );
            result = OperationResult<IReadOnlyList<MatoiPost>>.Fail(transportError, lastStatus);
        }
        else
        {
            result = OperationResult<IReadOnlyList<MatoiPost>>.Fail(
                "matoi не вернул ни одного поста с безопасным рейтингом",
                HttpStatusCode.NotFound
            );
        }

        return result;
    }

    /// <summary>
    /// Оставить посты безопасного рейтинга, у которых есть файл, и не больше
    /// <paramref name="limit"/>.
    /// </summary>
    /// <remarks>
    /// Фильтр по рейтингу здесь — не подстраховка ради подстраховки. Запрошенный
    /// тег матои подставляет в запрос, но набор значений рейтинга у провайдера
    /// может оказаться шире одного тега, и пропущенный пост дошёл бы до оверлея
    /// уже отфильтрованным только matoi.
    /// </remarks>
    private static void AppendSafe(
        List<MatoiPost> collected,
        HashSet<string> seen,
        IEnumerable<MatoiPost> posts,
        MatoiRating rating,
        int limit
    )
    {
        foreach (var post in posts)
        {
            if (collected.Count >= limit)
            {
                break;
            }

            var url = FirstFileUrl(post);

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var value = post.Rating?.Trim().ToLowerInvariant() ?? string.Empty;

            if (!rating.SafeRatings.Contains(value))
            {
                continue;
            }

            // Ключ — не только идентификатор: у разных страниц одного провайдера
            // идентификаторы не пересекаются, но matoi на одинаковый запрос
            // возвращает одну и ту же страницу, и без ключа оверлей получил бы
            // три копии одного изображения.
            if (seen.Add(url))
            {
                post.FileUrl = url;
                collected.Add(post);
            }
        }
    }

    private static string? FirstFileUrl(MatoiPost post)
    {
        if (!string.IsNullOrWhiteSpace(post.FileUrl))
        {
            return post.FileUrl;
        }

        if (!string.IsNullOrWhiteSpace(post.PreviewUrl))
        {
            return post.PreviewUrl;
        }

        return string.IsNullOrWhiteSpace(post.SampleUrl) ? null : post.SampleUrl;
    }

    private async Task<PageResult> ReadPageAsync(
        string provider,
        string tags,
        int limit,
        int page,
        CancellationToken cancellationToken
    )
    {
        PageResult result;

        try
        {
            var path =
                $"api/{Uri.EscapeDataString(provider)}/posts"
                + $"?tags={Uri.EscapeDataString(tags)}"
                + $"&limit={limit}"
                + $"&page={page}"
                + "&shuffle=true";

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new("Bearer", options.Value.ApiKey);
            request.Headers.Accept.Add(new("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                // Неизвестный провайдер и несуществующий тег дают один и тот же 404
                // с пустым телом, проверено на живом стенде. Отличать их нечем и
                // незачем: оба означают «нечем ответить».
                result = PageResult.Failed(
                    $"matoi не нашёл постов по тегу \"{tags}\" у провайдера {provider}",
                    HttpStatusCode.NotFound
                );
            }
            else if (!response.IsSuccessStatusCode)
            {
                result = PageResult.Failed(
                    $"matoi ответил {(int)response.StatusCode} на запрос постов"
                        + $" ({provider}, page={page})",
                    response.StatusCode
                );
            }
            else if (string.IsNullOrWhiteSpace(body))
            {
                // Было на живом стенде: 200 с пустым телом на валидный запрос.
                // Это молчащий обрыв, а не пустая выдача, и молча пропустить его
                // нельзя — иначе поломка стенда выглядит как «ничего не нашлось».
                result = PageResult.Failed(
                    "matoi вернул пустой ответ на запрос постов",
                    HttpStatusCode.BadGateway
                );
            }
            else
            {
                var envelope = JsonSerializer.Deserialize<MatoiPostsResponse>(body, JsonOptions);

                result = PageResult.FromEnvelope(envelope, provider, tags);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException)
        {
            result = PageResult.Failed(
                $"Не удалось обратиться к matoi: {exception.Message}",
                HttpStatusCode.BadGateway
            );
        }

        return result;
    }

    /// <summary>Разобранная страница либо причина, по которой её нет.</summary>
    private sealed record PageResult(MatoiPost[]? Posts, string? Error, HttpStatusCode Status)
    {
        public static PageResult FromEnvelope(
            MatoiPostsResponse? envelope,
            string provider,
            string tags
        )
        {
            PageResult result;

            if (envelope?.Posts is null)
            {
                result = PageResult.Failed(
                    $"matoi вернул ответ без списка постов ({provider}, tags={tags})",
                    HttpStatusCode.BadGateway
                );
            }
            else
            {
                result = new PageResult(envelope.Posts, null, HttpStatusCode.OK);
            }

            return result;
        }

        public static PageResult Failed(string error, HttpStatusCode status) =>
            new(null, error, status);
    }
}
