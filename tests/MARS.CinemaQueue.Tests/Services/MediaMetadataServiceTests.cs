using MARS.CinemaQueue.Models;
using MARS.CinemaQueue.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.CinemaQueue.Tests.Services;

/// <summary>
/// Метаданные фильма по ссылке.
///
/// Проверяется, что название собирается с годом, а источник сохраняется в
/// метаданных: очередь показывает карточку фильма, и год в названии — часть того,
/// что видит зритель.
/// </summary>
public class MediaMetadataServiceTests
{
    private const string Url = "https://www.kinopoisk.ru/film/301";

    [Fact]
    public async Task TitleGetsTheYear()
    {
        var service = Create(Movie(name: "Начало", year: 2010));

        var metadata = await service.GetMetadataAsync(Url, Token);

        Assert.Equal("Начало (2010)", metadata!.Title);
        Assert.Equal(Url, metadata.SourceUrl);
    }

    /// <summary>
    /// Фильм без года остаётся с названием: пустой год в названии выглядел бы как
    /// «Начало ()».
    /// </summary>
    [Fact]
    public async Task TitleWithoutYearIsKept()
    {
        var service = Create(Movie(name: "Начало", year: null));

        var metadata = await service.GetMetadataAsync(Url, Token);

        Assert.Equal("Начало", metadata!.Title);
    }

    /// <summary>
    /// Описание берётся из полного, а если его нет — из короткого.
    /// </summary>
    [Fact]
    public async Task DescriptionFallsBackToShort()
    {
        var service = Create(
            Movie(name: "Начало", year: 2010, description: null, shortDescription: "Сон")
        );

        var metadata = await service.GetMetadataAsync(Url, Token);

        Assert.Equal("Сон", metadata!.Description);
    }

    [Fact]
    public async Task PosterIsTakenFromApi()
    {
        var service = Create(Movie(name: "Начало", year: 2010, poster: "https://img.test/1.jpg"));

        var metadata = await service.GetMetadataAsync(Url, Token);

        Assert.Equal("https://img.test/1.jpg", metadata!.ImageUrl);
    }

    /// <summary>
    /// Пустое название бесполезно: карточку нечем подписать.
    /// </summary>
    [Fact]
    public async Task FilmWithoutNameIsRejected()
    {
        var service = Create(Movie(name: "   ", year: 2010));

        Assert.Null(await service.GetMetadataAsync(Url, Token));
    }

    /// <summary>
    /// Неизвестный источник не опрашивается: что подставить вместо названия, по
    /// чужой ссылке неизвестно.
    /// </summary>
    [Theory]
    [InlineData("https://example.test/film/301")]
    [InlineData("")]
    [InlineData(null)]
    public async Task UnsupportedSourceYieldsNoMetadata(string? url)
    {
        var kinopoisk = new Mock<IKinopoiskService>();
        var service = new MediaMetadataService(
            kinopoisk.Object,
            NullLogger<MediaMetadataService>.Instance
        );

        Assert.Null(await service.GetMetadataAsync(url!, Token));
        Assert.Empty(kinopoisk.Invocations);
    }

    /// <summary>
    /// Фильм, которого нет в Кинопоиске, остаётся без метаданных, а не падает.
    /// </summary>
    [Fact]
    public async Task UnknownFilmYieldsNoMetadata()
    {
        var service = Create(null);

        Assert.Null(await service.GetMetadataAsync(Url, Token));
    }

    /// <summary>
    /// Сбой Кинопоиска не роняет очередь: карточка просто останется пустой.
    /// </summary>
    [Fact]
    public async Task ApiFailureYieldsNoMetadata()
    {
        var kinopoisk = new Mock<IKinopoiskService>();
        kinopoisk
            .Setup(service =>
                service.GetMovieByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new HttpRequestException("Кинопоиск недоступен"));
        var service = new MediaMetadataService(
            kinopoisk.Object,
            NullLogger<MediaMetadataService>.Instance
        );

        Assert.Null(await service.GetMetadataAsync(Url, Token));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static MediaMetadataService Create(KinopoiskMovieDto? movie)
    {
        var kinopoisk = new Mock<IKinopoiskService>();
        kinopoisk
            .Setup(service =>
                service.GetMovieByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(movie);

        return new MediaMetadataService(
            kinopoisk.Object,
            NullLogger<MediaMetadataService>.Instance
        );
    }

    private static KinopoiskMovieDto Movie(
        string? name,
        int? year,
        string? description = null,
        string? shortDescription = null,
        string? poster = null
    ) =>
        new()
        {
            Name = name,
            Year = year,
            Description = description,
            ShortDescription = shortDescription,
            Poster = poster is null ? null : new KinopoiskPoster { Url = poster },
        };
}
