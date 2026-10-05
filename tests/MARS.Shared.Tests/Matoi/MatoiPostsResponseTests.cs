using System.Text.Json;
using MARS.Shared.Matoi;

namespace MARS.Shared.Tests.Matoi;

/// <summary>
/// Разбор настоящего ответа matoi 15.7.0-alpha.
/// </summary>
/// <remarks>
/// Тест без сети: тело записано с живого стенда
/// (<c>GET /api/danbooru/posts?tags=rating:g&amp;limit=2</c> с ключом) и урезано
/// по длине тегов, но не по названиям полей. Смысл в том, чтобы переименование
/// поля у matoi ловилось здесь, а не в оверлее: неизвестное поле
/// <see cref="JsonSerializer"/> игнорирует молча, и модель тихо останется пустой —
/// без такого теста это выглядело бы как «matoi сломался».
/// </remarks>
public class MatoiPostsResponseTests
{
    [Fact]
    public void НастоящийОтветРазбираетсяВМодель()
    {
        var response = Envelope();

        Assert.True(response.Success);
        Assert.Equal("danbooru", response.Provider);
        Assert.Equal(2, response.Count);
        Assert.Equal(2, Posts().Length);
    }

    [Fact]
    public void СсылкаНаФайлБерётсяИзИсточникаАНеИзКопииMatoi()
    {
        // У ответа есть и matoi_file_url — копия в самом контейнере. Оверлею она
        // не годится: наружу она не отдаётся, и ссылка была бы битой. Выбирается
        // исходная, и тест падает, если модель начнёт читать поле с префиксом matoi.
        var post = Posts()[0];

        Assert.Equal("https://cdn.donmai.us/original/cf/be/cfbe.mp4", post.FileUrl);
        Assert.DoesNotContain("matoi", post.FileUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void РейтингПриходитОтдельнымПолем()
    {
        // Безопасность проверяется по rating, и тега rating в tags не бывает:
        // matoi вырезает запрошенный тег из ответа.
        Assert.All(Posts(), post => Assert.Equal("g", post.Rating));
        Assert.All(
            Posts(),
            post => Assert.DoesNotContain("rating:g", post.Tags ?? [], StringComparer.Ordinal)
        );
    }

    [Fact]
    public void ФайлМожетБытьВидео()
    {
        // Награда RANDOM ART умеет показывать видео, а тип файла берётся из
        // расширения: отдельного поля с mime у ответа нет.
        Assert.EndsWith(".mp4", Posts()[0].FileUrl, StringComparison.Ordinal);
        Assert.EndsWith(".jpg", Posts()[1].FileUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void НеизвестныеПоляНеЛомаютРазбор()
    {
        var post = Posts()[0];

        Assert.Equal(12320251, post.Id);
        Assert.Equal(0, post.Score);
        Assert.Equal("https://danbooru.donmai.us/posts/12320251", post.Link);
        Assert.Contains("video", post.Tags ?? []);
    }

    /// <summary>
    /// Конверт записанного ответа.
    /// </summary>
    /// <remarks>
    /// Отдельный метод, а не разыменование результата <c>Deserialize</c> в тесте:
    /// возвращаемый тип nullable, и <c>Assert.NotNull</c> не передаёт компилятору,
    /// что ссылка не пуста, — сборка падала бы на CS8602 вместо проверки.
    /// </remarks>
    private static MatoiPostsResponse Envelope() =>
        JsonSerializer.Deserialize<MatoiPostsResponse>(RealDanbooruResponse)
        ?? throw new InvalidOperationException("Записанный ответ matoi не разобрался.");

    /// <summary>
    /// Массив постов из записанного ответа, уже с проверкой наличия.
    /// </summary>
    private static MatoiPost[] Posts() =>
        Envelope().Posts ?? throw new InvalidOperationException("В записанном ответе нет posts.");

    /// <summary>
    /// Ответ стенда. Лишние клиенту поля (<c>directory</c>, <c>image</c>,
    /// <c>source</c>, <c>matoi_file_url</c>) оставлены намеренно: тест должен
    /// ловить и переименование поля, и появление новых, а не только свой набор.
    /// </summary>
    private const string RealDanbooruResponse = """
        {
          "success": true,
          "provider": "danbooru",
          "count": 2,
          "posts": [
            {
              "id": 12320251,
              "rating": "g",
              "file_url": "https://cdn.donmai.us/original/cf/be/cfbe.mp4",
              "preview_url": "https://cdn.donmai.us/180x180/cf/be/cfbe.jpg",
              "sample_url": "https://cdn.donmai.us/original/cf/be/cfbe.mp4",
              "score": 0,
              "link": "https://danbooru.donmai.us/posts/12320251",
              "tags": ["video", "animated", "blue_archive"],
              "directory": 0,
              "image": null,
              "source": "danbooru",
              "matoi_file_url": "/data/matoi/cfbe.mp4"
            },
            {
              "id": 12320250,
              "rating": "g",
              "file_url": "https://cdn.donmai.us/original/be/f1/bef1.jpg",
              "preview_url": "https://cdn.donmai.us/180x180/be/f1/bef1.jpg",
              "sample_url": "https://cdn.donmai.us/original/be/f1/bef1.jpg",
              "score": 1,
              "link": "https://danbooru.donmai.us/posts/12320250",
              "tags": ["keroro", "horns"],
              "directory": 0,
              "image": null,
              "source": "danbooru",
              "matoi_file_url": "/data/matoi/bef1.jpg"
            }
          ]
        }
        """;
}
