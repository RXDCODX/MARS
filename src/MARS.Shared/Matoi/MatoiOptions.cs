namespace MARS.Shared.Matoi;

/// <summary>
/// Настройки клиента matoi. Секция конфигурации — <c>Matoi</c>.
/// </summary>
public sealed class MatoiOptions
{
    public const string SectionName = "Matoi";

    /// <summary>
    /// Базовый адрес matoi. В стенде это имя контейнера
    /// (<c>http://matoi:3000</c>): наружу сервис не публикуется, поэтому
    /// вызывать его можно только изнутри <c>mars-network</c>.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ключ авторизации. Передаётся как <c>Authorization: Bearer</c>.
    /// </summary>
    /// <remarks>
    /// Пустое значение означает, что matoi вообще не включил авторизацию
    /// (проверка на сервере сравнивает <c>configuredKey</c> с пустой строкой), а
    /// не «открытый, но без ключа». Поэтому пустой ключ клиент считает
    /// ошибкой настройки и никуда не ходит.
    /// </remarks>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Провайдер по умолчанию, когда ввод награды не содержит <c>provider:</c>.</summary>
    public string DefaultProvider { get; set; } = "danbooru";

    /// <summary>
    /// Потолок <c>limit</c>. На живом стенде matoi отдавал не больше 200 постов
    /// на страницу, а <c>limit=500</c> отдавал столько же, сколько 200, — то
    /// есть потолок применялся молча.
    /// </summary>
    public int MaxLimit { get; set; } = 200;

    /// <summary>
    /// Потолок номера страницы при выборе случайной.
    /// </summary>
    /// <remarks>
    /// Нужен, потому что страница за пределами выдачи — не 404, а 500: на живом
    /// стенде <c>page=100000</c> вернул <c>500 Internal Server Error</c>.
    /// </remarks>
    public int MaxPage { get; set; } = 100;

    /// <summary>
    /// Сколько страниц подряд обходить в поисках достаточного количества.
    /// </summary>
    /// <remarks>
    /// Нужен потому, что matoi режет выдачу и не дополняет её: на стенде
    /// <c>limit=20</c> приходили 19 постов, <c>limit=100</c> — 97, а
    /// <c>limit=200</c> — 195. Поле <c>count</c> в ответе не является
    /// обещанием, поэтому недостающее дотягивается со следующих страниц.
    /// </remarks>
    public int PageAttempts { get; set; } = 5;

    /// <summary>
    /// Таймаут запроса. Верхний разумный: matoi сам ходит к booru, и при
    /// недоступном источнике отвечает долго.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 20;
}
