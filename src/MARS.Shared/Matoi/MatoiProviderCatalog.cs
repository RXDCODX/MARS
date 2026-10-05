namespace MARS.Shared.Matoi;

/// <summary>
/// Рейтинговый словарь одного провайдера: какой тег просить и какие значения
/// рейтинга считать безопасными.
/// </summary>
/// <param name="SafeRequestTag">Тег, который добавляется к запросу.</param>
/// <param name="SafeRatings">
/// Значения поля <c>rating</c>, которые считаются безопасными. Пустое множество
/// означает «ничего не считать безопасным», а не «считать всё безопасным».
/// </param>
public sealed record MatoiRating(string SafeRequestTag, IReadOnlySet<string> SafeRatings);

/// <summary>
/// Словари рейтингов по провайдерам matoi.
/// </summary>
/// <remarks>
/// Словарь закрытый, и это осознанно: у booru рейтинг не унифицирован, а ошибка
/// в переводе — это чувствительное в оверлее, а не дефект оформления.
/// Например, на danbooru тег <c>rating:safe</c> означает sensitive и на живом
/// стенде вернул 100 постов из 100 с рейтингом <c>s</c>; безопасным там является
/// <c>rating:g</c>. У rule34 шкала другая, и safe там — это <c>s</c>.
/// Провайдер добавляется сюда только после проверки на живом стенде.
/// </remarks>
public static class MatoiProviderCatalog
{
    /// <summary>
    /// Провайдер, который не описан: безопасных значений ноль, тег запроса
    /// пустой. Так словарь отвечает на вопрос «а что насчёт gelbooru?» честным
    /// «ничего не известно» вместо правдоподобного значения по умолчанию.
    /// </summary>
    private static readonly MatoiRating Unknown = new(string.Empty, new HashSet<string>());

    private static readonly Dictionary<string, MatoiRating> Ratings = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["danbooru"] = new MatoiRating("rating:g", new HashSet<string>(["g"])),
        // Требует ключей источника: без них DAPI отвечает отказом, и matoi
        // пробрасывает это как 502. При настроенных ключах шкала rule34 такова:
        // s — safe, g — general, q — questionable, e — explicit.
        ["rule34"] = new MatoiRating("rating:s", new HashSet<string>(["s"])),
    };

    /// <summary>
    /// Словарь провайдера. Имя сравнивается без учёта регистра и краёв: приходит
    /// оно из пользовательского ввода награды.
    /// </summary>
    public static bool TryGet(string? provider, out MatoiRating rating)
    {
        rating = Unknown;

        if (!string.IsNullOrWhiteSpace(provider))
        {
            var name = provider.Trim();

            if (Ratings.TryGetValue(name, out var found))
            {
                rating = found;
            }
        }

        return Ratings.ContainsKey(provider?.Trim() ?? string.Empty);
    }

    /// <summary>Все описанные провайдеры — для диагностики и сообщений об ошибке.</summary>
    public static IReadOnlyCollection<string> KnownProviders { get; } = Ratings.Keys.ToArray();
}
