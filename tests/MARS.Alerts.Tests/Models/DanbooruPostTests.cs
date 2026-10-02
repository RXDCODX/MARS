using MARS.Alerts.Models;

namespace MARS.Alerts.Tests.Models;

/// <summary>
/// Разбор ответа Danbooru.
///
/// Проверяется, что приходит правильный тип даты и что вложенный медиафайл
/// разбирается вместе с вариантами: без него в оповещении не было бы превью, а
/// публикация осталась бы пустой.
/// </summary>
public class DanbooruPostTests
{
    [Fact]
    public void SinglePostIsWrappedIntoArray()
    {
        var posts = DanbooruPost.FromJson("""{"id":5,"file_ext":"png","md5":"abc"}""");

        Assert.NotNull(posts);
        Assert.Equal([5], posts!.Select(post => post.Id));
        Assert.Equal("png", posts[0].FileExt);
    }

    [Fact]
    public void ArrayOfPostsIsParsed()
    {
        var posts = DanbooruPost.FromJson("""[{"id":1},{"id":2}]""");

        Assert.Equal([1, 2], posts!.Select(post => post.Id));
    }

    /// <summary>
    /// Не-объект в массиве пропускается, а не ломает разбор всего ответа: в выдаче
    /// Danbooru попадаются служебные элементы.
    /// </summary>
    [Fact]
    public void NonObjectItemsAreSkipped()
    {
        var posts = DanbooruPost.FromJson("""[{"id":1},"просто текст"]""");

        Assert.Equal([1], posts!.Select(post => post.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyJsonYieldsNoPosts(string? json)
    {
        Assert.Null(DanbooruPost.FromJson(json!));
    }

    [Fact]
    public void BrokenJsonYieldsNoPosts()
    {
        Assert.Null(DanbooruPost.FromJson("{ это не json"));
    }

    [Fact]
    public void ScalarJsonYieldsNoPosts()
    {
        Assert.Null(DanbooruPost.FromJson("42"));
    }

    /// <summary>
    /// Даты приходят в формате Danbooru, а модель хранит <see cref="DateTime"/>:
    /// разбор должен наполнить её, иначе сортировка по дате ломалась бы.
    /// </summary>
    [Fact]
    public void DatesAreParsed()
    {
        var post = DanbooruPost.FromJson(
            """{"id":1,"created_at":"2023-05-06T07:08:09.000+09:00"}"""
        )![0];

        Assert.NotNull(post.CreatedAt);
        Assert.Equal(2023, post.CreatedAt!.Value.Year);
    }

    [Theory]
    [InlineData("""{"id":1,"created_at":null}""")]
    [InlineData("""{"id":1}""")]
    [InlineData("""{"id":1,"created_at":"не дата"}""")]
    public void MissingOrBrokenDateIsNull(string json)
    {
        Assert.Null(DanbooruPost.FromJson(json)![0].CreatedAt);
    }

    [Fact]
    public void MediaAssetIsParsedWithVariants()
    {
        var post = DanbooruPost.FromJson(
            """
            {"id":1,"media_asset":{"id":9,"file_ext":"mp4","status":"active","variants":[{"type":"webm","url":"/v.webm","width":640,"height":360}]}}
            """
        )![0];

        Assert.NotNull(post.MediaAsset);
        Assert.Equal(9, post.MediaAsset!.Id);
        Assert.Equal("active", post.MediaAsset.Status);
        var variant = Assert.Single(post.MediaAsset.Variants!);
        Assert.Equal("webm", variant.Type);
        Assert.Equal(640, variant.Width);
    }

    [Fact]
    public void PostWithoutMediaAssetHasNone()
    {
        Assert.Null(DanbooruPost.FromJson("""{"id":1}""")![0].MediaAsset);
    }

    /// <summary>
    /// Теги и оценки нужны фильтрам постинга: без их разбора в посте не было бы
    /// ни рейтинга, ни списка тегов.
    /// </summary>
    [Fact]
    public void TagsAndScoresAreParsed()
    {
        var post = DanbooruPost.FromJson(
            """{"id":1,"score":42,"tag_count":7,"tag_string":"1girl solo","tag_string_artist":"nashi"}"""
        )![0];

        Assert.Equal(42, post.Score);
        Assert.Equal(7, post.TagCount);
        Assert.Equal("1girl solo", post.TagString);
        Assert.Equal("nashi", post.TagStringArtist);
    }
}
