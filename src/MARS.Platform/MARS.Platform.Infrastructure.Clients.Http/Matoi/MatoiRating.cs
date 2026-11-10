namespace MARS.Platform.Infrastructure.Clients.Http.Matoi;

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
