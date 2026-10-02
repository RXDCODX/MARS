using System.Reflection;
using MARS.WaifuGacha.Controllers;

namespace MARS.WaifuGacha.Tests.Controllers;

/// <summary>
/// Адреса картинок вайфу.
///
/// В базе картинка хранится относительным путём, а фронтенд получает абсолютный.
/// Проверяется, что сайт не приклеивается дважды: без проверки префикса ссылка
/// выглядела бы как <c>https://shikimori.tkhikimori.tk/images/1.png</c> и картинка не
/// открывалась бы.
/// </summary>
public class WaifuImageUrlTests
{
    private const string Site = "https://shikimori.tk";

    [Fact]
    public void RelativeUrlGetsSitePrefix()
    {
        Assert.Equal($"{Site}/images/1.png", Fix("/images/1.png", Site));
    }

    [Fact]
    public void AbsoluteUrlIsKept()
    {
        Assert.Equal($"{Site}/images/1.png", Fix($"{Site}/images/1.png", Site));
    }

    [Fact]
    public void ExternalUrlGetsPrefixToo()
    {
        Assert.Equal($"{Site}https://other.test/1.png", Fix("https://other.test/1.png", Site));
    }

    /// <summary>
    /// Пустой адрес остаётся пустым: без проверки в базу попадал бы путь на сам сайт.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyUrlIsNotPrefixed(string? url)
    {
        Assert.Equal("", Fix(url!, Site));
        Assert.Equal("", Normalize(url!, Site));
    }

    /// <summary>
    /// Приведение к относительному пути убирает сайт: так картинки хранятся в базе
    /// и не зависят от адреса стенда.
    /// </summary>
    [Fact]
    public void AbsoluteUrlIsReducedToPath()
    {
        Assert.Equal("/images/1.png", Normalize($"{Site}/images/1.png", Site));
    }

    [Fact]
    public void RelativeUrlIsNotChangedByNormalize()
    {
        Assert.Equal("/images/1.png", Normalize("/images/1.png", Site));
    }

    /// <summary>
    /// Регистр адреса не важен: стенд и база могут отличаться регистром.
    /// </summary>
    [Fact]
    public void PrefixMatchIgnoresCase()
    {
        Assert.Equal(
            "https://SHIKIMORI.TK/images/1.png",
            Fix("https://SHIKIMORI.TK/images/1.png", Site)
        );
    }

    private static string Fix(string? imageUrl, string site) => Call("FixImageUrl", imageUrl, site);

    private static string Normalize(string? imageUrl, string site) =>
        Call("NormalizeImageUrl", imageUrl, site);

    private static string Call(string name, string? imageUrl, string site)
    {
        var method = typeof(WaifuRollController).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (string)method.Invoke(null, [imageUrl, site])!;
    }
}
