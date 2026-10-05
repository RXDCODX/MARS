using System.Globalization;
using System.Net;
using System.Text.Json;
using MARS.Shared.Matoi;
using MARS.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Matoi;

/// <summary>
/// Клиент matoi: маршрут, авторизация, перевод тега рейтинга и фильтрация по
/// рейтингу. Реальный matoi не нужен — <see cref="StubHandler"/> отвечает
/// заданным JSON.
/// </summary>
/// <remarks>
/// Ожидаемое поведение выведено из живых прогонов
/// <c>ghcr.io/sinkaroid/matoi:15.7.0-alpha</c>. Две проверки ниже — прямые
/// последствия того, что там обнаружилось:
/// <list type="bullet">
/// <item>на danbooru тег <c>rating:safe</c> возвращает рейтинг <c>s</c>
/// (sensitive), а не safe: 100 постов из 100. Без перевода на <c>rating:g</c>
/// в оверлей попадает чувствительное, то есть это дефект модерации, а не
/// мелочь оформления;</item>
/// <item>тег <c>rating:</c> вырезается из ответа — в <c>tags</c> его нет ни у
/// одного поста, — поэтому проверять безопасность по тегам нельзя, только по
/// полю <c>rating</c>.</item>
/// </list>
/// </remarks>
public class MatoiPostServiceTests
{
    [Fact]
    public async Task ЗапросИдётПоПровайдерномуМаршрутуСКлючом()
    {
        var handler = Stub(Envelope(("g", 7)));
        var service = Create(handler, apiKey: "секретный-ключ");

        await service.GetSafePostsAsync("danbooru", "hatsune_miku", 3, Token);

        // Берётся первый запрос, а не единственный: клиент догоняет нехватку
        // постов со следующих страниц, поэтому запросов бывает несколько.
        var request = handler.Requests[0];

        Assert.Equal("/api/danbooru/posts", request.RequestUri?.AbsolutePath);
        Assert.Equal("секретный-ключ", request.Headers.Authorization?.Parameter);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
    }

    [Fact]
    public async Task ПустойКлючНеОтправляетЗапрос()
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler, apiKey: "   ");

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
        Assert.Contains("ключ", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// На danbooru безопасный тег — <c>rating:g</c>, и <c>rating:safe</c> там
    /// означает sensitive.
    /// </summary>
    [Fact]
    public async Task DanbooruЗапрашиваетсяСРейтингомGeneral()
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler);

        await service.GetSafePostsAsync("danbooru", "hatsune_miku", 3, Token);

        // Сверяется декодированное значение: клиент экранирует тег, и двоеточие
        // рейтинга уезжает как %3A. На живом стенде такая форма разбирается так
        // же, как литерал, — проверено отдельно.
        var tags = QueryValue(handler.Requests[0], "tags");

        Assert.Contains("rating:g", tags, StringComparison.Ordinal);
        Assert.DoesNotContain("rating:safe", tags, StringComparison.Ordinal);
        Assert.Contains("hatsune_miku", tags, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rule34ЗапрашиваетсяСРейтингомSafe()
    {
        var handler = Stub(Envelope(("s", 1)));
        var service = Create(handler);

        await service.GetSafePostsAsync("rule34", "tag", 3, Token);

        var tags = QueryValue(handler.Requests[0], "tags");

        Assert.Contains("rating:s", tags, StringComparison.Ordinal);
        Assert.DoesNotContain("rating:g", tags, StringComparison.Ordinal);
    }

    /// <summary>
    /// Провайдер без описанного словаря рейтингов отклоняется до похода в сеть.
    /// </summary>
    /// <remarks>
    /// Словарь пришлось бы угадывать, а ошибка в нём — это sensitive в оверлее.
    /// Лучше честный отказ с внятным текстом, чем правдоподобный тег.
    /// </remarks>
    [Fact]
    public async Task НеизвестныйПровайдерОтклоняетсяБезЗапроса()
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("some_new_booru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
        Assert.Contains("some_new_booru", result.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Второй рубеж: посты чужого рейтинга отбрасываются, даже если matoi их
    /// вернул.
    /// </summary>
    [Fact]
    public async Task ЧужойРейтингОтбрасываетсяНаКлиенте()
    {
        var handler = Stub(Envelope(("g", 1), ("s", 2), ("q", 3), ("e", 4), ("g", 5), ("s", 6)));
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 10, Token);

        Assert.True(result.Success);
        Assert.Equal([1L, 5L], result.Result!.Select(post => post.Id));
    }

    [Fact]
    public async Task ЧужоеЗначениеРейтингаНеПутаетсяСДругимПровайдером()
    {
        // rating:s на rule34 — это safe, а на danbooru то же значение означало бы
        // sensitive. Словарь рейтингов у провайдеров разный, и общий список
        // «допустимых» значений унёс бы модерацию в оверлей.
        var handler = Stub(Envelope(("s", 1), ("g", 2)));
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("rule34", "tag", 10, Token);

        Assert.True(result.Success);
        Assert.Equal([1L], result.Result!.Select(post => post.Id));
    }

    [Fact]
    public async Task ВозвращаетсяНеБольшеЗапрошенного()
    {
        var handler = Stub(Envelope(("g", 1), ("g", 2), ("g", 3), ("g", 4)));
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 2, Token);

        Assert.True(result.Success);
        Assert.Equal(2, result.Result!.Count);
    }

    /// <summary>
    /// Клиент не должен полагаться на точный счёт: matoi режет выдачу.
    /// </summary>
    /// <remarks>
    /// На живом стенде <c>limit=20</c> возвращал 19 постов, <c>limit=100</c> —
    /// 97, а <c>limit=200</c> — 195. Считать по <c>count</c> из ответа и
    /// запрашивать недостающее «ещё раз» бессмысленно.
    /// </remarks>
    [Fact]
    public async Task МалоеКоличествоНеСчитаетсяОшибкой()
    {
        var handler = Stub(
            """
            {"success":true,"provider":"danbooru","count":1,"posts":[{"id":9,"rating":"g","file_url":"https://cdn/1.png"}]}
            """
        );
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.True(result.Success);
        Assert.Single(result.Result!);
    }

    [Fact]
    public async Task ПустоеТелоОтветаСчитаетсяОшибкой()
    {
        // На живом стенде matoi один раз отдал пустое тело на валидный запрос:
        // результат был 200-типичным, постов ноль. Молча считать это «ничего не
        // нашлось» нельзя — это молчащий обрыв, а пустая выдача приходит иначе.
        var handler = Stub(string.Empty);
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Contains("пуст", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Отсутствие постов приходит как 404 с пустым телом, а не конвертом с
    /// <c>posts: []</c>.
    /// </summary>
    /// <remarks>
    /// Проверено на живом стенде дважды: несуществующий тег и несуществующий
    /// провайдер дают один и тот же 404 без тела. Значит 404 — это «нечем
    /// ответить», а не «ответ пуст», и это не повод пускать содержимое в оверлей.
    /// </remarks>
    [Fact]
    public async Task ОтсутствиеПостовПриходитКак404()
    {
        var handler = Stub(string.Empty, HttpStatusCode.NotFound);
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task ОшибкаСервераНеПутаетсяСПустымОтветом()
    {
        var handler = Stub(string.Empty, HttpStatusCode.InternalServerError);
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task ОшибкаАвторизацииВиднаВСообщении()
    {
        var handler = Stub(string.Empty, HttpStatusCode.Unauthorized);
        var service = Create(handler, apiKey: "неверный-ключ");

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task ОбрывТранспортаНеБросаетИсключениеНаружу()
    {
        var service = Create(new ThrowingHandler(new HttpRequestException("обрыв")));

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.False(result.Success);
        Assert.Contains("обрыв", result.ErrorMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Если на случайной странице постов не хватило, клиент идёт дальше.
    /// </summary>
    /// <remarks>
    /// Без этого оверлей почти всегда получал бы меньше картинок, чем обещает
    /// награда: matoi режет выдачу, и <c>count</c> из ответа — не обещание.
    /// </remarks>
    [Fact]
    public async Task НехваткаДотягиваетсяСоСледующейСтраницы()
    {
        var handler = new StubHandler(
            sequence: [() => Ok(Envelope(("g", 1))), () => Ok(Envelope(("g", 2), ("g", 3)))]
        );
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 3, Token);

        Assert.True(result.Success);
        Assert.Equal([1L, 2L, 3L], result.Result!.Select(post => post.Id));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ОбходСтраницОграниченЧисломПопыток()
    {
        var handler = new StubHandler(sequence: [() => Ok(Envelope(("g", 1)))]);
        var service = Create(handler, pageAttempts: 3);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 10, Token);

        Assert.True(result.Success);
        Assert.Equal(3, handler.Requests.Count);
    }

    /// <summary>
    /// Страница выбирается случайно и остаётся в пределах настроенного потолка.
    /// </summary>
    /// <remarks>
    /// Случайная страница вместо всегда первой — единственный способ получить
    /// новые посты: <c>shuffle=true</c> меняет порядок внутри кэшированной
    /// страницы, но не её состав (проверено на живом стенде: три запроса одной
    /// страницы вернули одну и ту же выборку в разном порядке).
    /// </remarks>
    [Fact]
    public async Task СтраницаОстаётсяВПределахПотолка()
    {
        var pages = new HashSet<int>();

        for (var attempt = 0; attempt < 12; attempt++)
        {
            var handler = Stub(Envelope(("g", 1)));
            var service = Create(handler, maxPage: 20);

            await service.GetSafePostsAsync("danbooru", "tag", 1, Token);

            var page = int.Parse(
                QueryValue(handler.Requests[0], "page"),
                CultureInfo.InvariantCulture
            );
            Assert.InRange(page, 1, 20);
            pages.Add(page);
        }

        // Случайность обязана быть настоящей: если бы страница была всегда
        // первой, набор из 12 номеров состоял бы из одного элемента.
        Assert.True(
            pages.Count > 1,
            $"Страница не менялась между вызовами: {string.Join(',', pages)}"
        );
    }

    [Fact]
    public async Task LimitОграниченПотолкомMatoi()
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler, maxLimit: 200);

        await service.GetSafePostsAsync("danbooru", "tag", 5000, Token);

        var query = Query(handler.Requests[0]);
        Assert.Contains("limit=200", query, StringComparison.Ordinal);
        Assert.DoesNotContain("limit=5000", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task НекорректныйLimitНеЛомаетЗапрос()
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 0, Token);

        Assert.True(result.Success);
        Assert.Contains("limit=1", Query(handler.Requests[0]), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ПустойТегОтклоняетсяДоЗапроса(string tags)
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", tags, 3, Token);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ТегЭкранируется()
    {
        var handler = Stub(Envelope(("g", 1)));
        var service = Create(handler);

        await service.GetSafePostsAsync("danbooru", "a b&c=d", 1, Token);

        // Тег приходит из пользовательского ввода, поэтому пробел и амперсанд
        // обязаны уехать закодированными. Проверяется сырой query: на нём видно
        // именно то, что уйдёт по сети.
        var raw = handler.Requests[0].RequestUri!.Query;

        Assert.DoesNotContain("a b&c=d", raw, StringComparison.Ordinal);
        Assert.Contains("a%20b%26c%3Dd", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ФайлБерётсяИзFileUrl()
    {
        var handler = Stub(
            """
            {"success":true,"provider":"danbooru","count":1,"posts":[
              {"id":1,"rating":"g","file_url":"https://cdn.example/post.png","sample_url":"https://cdn.example/s.jpg"}]}
            """
        );
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 1, Token);

        Assert.Equal("https://cdn.example/post.png", result.Result!.Single().FileUrl);
    }

    [Fact]
    public async Task ПостБезФайлаНеПопадаетВВыдачу()
    {
        var handler = Stub(
            """
            {"success":true,"provider":"danbooru","count":2,"posts":[
              {"id":1,"rating":"g"},
              {"id":2,"rating":"g","file_url":"https://cdn.example/post.png"}]}
            """
        );
        var service = Create(handler);

        var result = await service.GetSafePostsAsync("danbooru", "tag", 10, Token);

        Assert.Equal([2L], result.Result!.Select(post => post.Id));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static MatoiPostService Create(
        HttpMessageHandler handler,
        string apiKey = "mars-dev-matoi-key",
        int maxLimit = 200,
        int maxPage = 100,
        int pageAttempts = 5
    )
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://matoi:3000/"),
            Timeout = TimeSpan.FromSeconds(20),
        };

        return new MatoiPostService(
            client,
            Options.Create(
                new MatoiOptions
                {
                    BaseUrl = "http://matoi:3000/",
                    ApiKey = apiKey,
                    DefaultProvider = "danbooru",
                    MaxLimit = maxLimit,
                    MaxPage = maxPage,
                    PageAttempts = pageAttempts,
                }
            ),
            NullLogger<MatoiPostService>.Instance
        );
    }

    private static StubHandler Stub(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(body, status);

    private static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };

    private static string Envelope(params (string Rating, long Id)[] posts)
    {
        var items = posts.Select(post =>
            $$"""{"id":{{post.Id}},"rating":"{{post.Rating}}","file_url":"https://cdn.example/{{post.Id}}.png"}"""
        );

        return $$"""{"success":true,"provider":"danbooru","count":{{posts.Length}},"posts":[{{string.Join(',', items)}}]}""";
    }

    private static string Query(HttpRequestMessage request) =>
        request.RequestUri?.Query ?? string.Empty;

    /// <summary>Значение одного параметра query.</summary>
    /// <remarks>
    /// Разбор идёт по разделителям, а не по <c>IndexOf("page=")</c>: номер
    /// страницы может оказаться многозначным, и срез по шести символам молча
    /// прочитал бы мусор вместо числа.
    /// </remarks>
    private static string QueryValue(HttpRequestMessage request, string name)
    {
        var query = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var separator = part.IndexOf('=');

            if (separator > 0 && part[..separator] == name)
            {
                // Значение возвращается декодированным: клиент экранирует тег, и
                // сравнение с неэкранированным ожиданием иначе всегда отличалось бы.
                return Uri.UnescapeDataString(part[(separator + 1)..]);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Заглушка HttpMessageHandler.
    /// </summary>
    /// <remarks>
    /// Последовательность задаётся фабриками, а не готовыми ответами: клиент
    /// освобождает ответ через <c>using</c>, и повторная выдача того же экземпляра
    /// упала бы с <c>ObjectDisposedException</c> вместо проверяемого результата.
    /// </remarks>
    private sealed class StubHandler(
        string body = "",
        HttpStatusCode status = HttpStatusCode.OK,
        IReadOnlyList<Func<HttpResponseMessage>>? sequence = null
    ) : HttpMessageHandler
    {
        private readonly List<Func<HttpResponseMessage>> _sequence = sequence?.ToList() ?? [];
        private int _index;

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);

            if (_sequence.Count > 0)
            {
                // Сначала отдаётся текущий элемент, и только потом курсор сдвигается:
                // порядок наоборот съел бы первый ответ подготовленной
                // последовательности и проверка обхода считала бы чужие страницы.
                // Последний элемент отвечает на все последующие вызовы — обход
                // страниц не обязан упираться в конец набора.
                var fromSequence = _sequence[_index]();

                if (_index < _sequence.Count - 1)
                {
                    _index++;
                }

                fromSequence.RequestMessage = request;

                return Task.FromResult(fromSequence);
            }

            return Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(
                        body,
                        System.Text.Encoding.UTF8,
                        "application/json"
                    ),
                    RequestMessage = request,
                }
            );
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => throw exception;
    }
}
