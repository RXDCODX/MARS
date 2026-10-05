using MARS.Shared.Matoi;

namespace MARS.Shared.Tests.Matoi;

/// <summary>
/// Словарь рейтингов matoi по провайдерам.
/// </summary>
/// <remarks>
/// Проверяется не само значение как таковое, а то, откуда оно взято и почему
/// словарь закрытый: <c>rating:</c> у провайдеров не означает одно и то же.
/// </remarks>
public class MatoiProviderCatalogTests
{
    /// <summary>
    /// У danbooru <c>rating:safe</c> — это sensitive.
    /// </summary>
    /// <remarks>
    /// Проверено на живом стенде: <c>tags=rating:safe</c> вернул 100 постов из
    /// 100 с рейтингом <c>s</c>, а <c>rating:g</c> и <c>rating:general</c> — 100
    /// постов с рейтингом <c>g</c>. Наивный «безопасный» тег, одинаковый для
    /// всех booru, отправил бы в оверлей чувствительное.
    /// </remarks>
    [Fact]
    public void DanbooruИспользуетGeneralАНеSafe()
    {
        Assert.True(MatoiProviderCatalog.TryGet("danbooru", out var rating));

        Assert.Equal("rating:g", rating.SafeRequestTag);
        Assert.DoesNotContain("rating:safe", rating.SafeRequestTag, StringComparison.Ordinal);
        Assert.Equal(["g"], rating.SafeRatings);
    }

    /// <summary>У rule34 шкала другая: safe там — это <c>s</c>.</summary>
    [Fact]
    public void Rule34ИспользуетSafeКакЗначениеS()
    {
        Assert.True(MatoiProviderCatalog.TryGet("rule34", out var rating));

        Assert.Equal("rating:s", rating.SafeRequestTag);
        Assert.Equal(["s"], rating.SafeRatings);
        Assert.DoesNotContain("g", rating.SafeRatings);
    }

    [Theory]
    [InlineData("danbooru")]
    [InlineData("Rule34")]
    [InlineData("  danbooru  ")]
    public void ИмяПровайдераНечувствительноКРегиструИПробелам(string provider)
    {
        Assert.True(MatoiProviderCatalog.TryGet(provider, out _));
    }

    [Theory]
    [InlineData("gelbooru")]
    [InlineData("safebooru")]
    [InlineData("yandere")]
    [InlineData("")]
    [InlineData("danbooru2")]
    public void НепроверенныйПровайдерНеОписан(string provider)
    {
        // gelbooru и safebooru не описаны сознательно: с живого стенда они отдают
        // 502, словарь рейтингов не проверен, а угадывать его — значит рискнуть
        // оверлеем. Провайдер добавляется сюда после проверки на стенде.
        Assert.False(MatoiProviderCatalog.TryGet(provider, out var rating));
        Assert.Equal(string.Empty, rating.SafeRequestTag);
        Assert.Empty(rating.SafeRatings);
    }

    [Fact]
    public void УНеизвестногоПровайдераСловарьПустойАНеДопускаетВсё()
    {
        Assert.False(MatoiProviderCatalog.TryGet("unknown", out var rating));

        // Пустое множество безопасных значений — это fail closed: пустой словарь
        // обязан отклонить всё, а не пропустить всё.
        Assert.Empty(rating.SafeRatings);
    }

    [Fact]
    public void ИзвестныеПровайдерыПеречислены()
    {
        // Сообщение об ошибке должно быть исправимым по тексту: без перечня
        // «провайдер не описан» читается как поломка стенда.
        Assert.Equal(["danbooru", "rule34"], MatoiProviderCatalog.KnownProviders.Order());
    }
}
