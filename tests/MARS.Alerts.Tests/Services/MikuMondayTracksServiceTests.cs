using MARS.Alerts.Models;
using MARS.Alerts.Services.Twitch.Rewards;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Хранилище треков Miku Monday.
///
/// Проверяется, что пустое хранилище возвращает ошибку, а не трек: команда Miku
/// Monday на стриме без загруженных треков должна честно сказать об этом, а не
/// показать зрителю случайную запись.
/// </summary>
public class MikuMondayTracksServiceTests
{
    private readonly MikuMondayTracksService _service = new(
        NullLogger<MikuMondayTracksService>.Instance
    );

    /// <summary>
    /// Пустое хранилище — ошибка без трека. Иначе зритель получил бы «трек»,
    /// которого нет.
    /// </summary>
    [Fact]
    public async Task EmptyStorageReportsError()
    {
        var result = await _service.GetRandomTrackForUserAsync("123", "Pyro");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Track);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task TrackIsPickedFromStorage()
    {
        _service.UpdateTracks([Track(1), Track(2)]);

        var result = await _service.GetRandomTrackForUserAsync("123", "Pyro");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Track);
    }

    /// <summary>
    /// Выбранный трек не повторяется в списке доступных: иначе Miku Monday мог бы
    /// выбрать его же следующей неделей без смены.
    /// </summary>
    [Fact]
    public async Task ChosenTrackIsNotInAvailableList()
    {
        _service.UpdateTracks([Track(1), Track(2), Track(3)]);

        var result = await _service.GetRandomTrackForUserAsync("123", "Pyro");

        Assert.DoesNotContain(result.AvailableTracks, track => track.Id == result.Track!.Id);
        Assert.Equal(2, result.AvailableTracks.Count);
    }

    [Fact]
    public async Task AvailableTracksAreListed()
    {
        _service.UpdateTracks([Track(1), Track(2)]);

        var tracks = await _service.GetAvailableTracksAsync();

        Assert.Equal([1, 2], tracks.Select(track => track.Id).Order());
    }

    /// <summary>
    /// Обновление заменяет список целиком: треки приходят из RabbitMQ, и старые
    /// записи не должны жить вечно.
    /// </summary>
    [Fact]
    public async Task UpdateReplacesPreviousTracks()
    {
        _service.UpdateTracks([Track(1), Track(2)]);
        _service.UpdateTracks([Track(9)]);

        var tracks = await _service.GetAvailableTracksAsync();

        Assert.Equal([9], tracks.Select(track => track.Id));
    }

    /// <summary>
    /// Вызов для стримера идёт тем же путём, что и для пользователя: трек
    /// выбирается из того же хранилища.
    /// </summary>
    [Fact]
    public async Task StreamerAlsoGetsTrack()
    {
        _service.UpdateTracks([Track(5)]);

        var result = await _service.GetRandomTrackForStreamerAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Track!.Id);
    }

    /// <summary>
    /// Старт безвреден и повторный вызов тоже: флаг инициализации статический, и
    /// повторный вызов не должен ни падать, ни перезагружать хранилище.
    /// </summary>
    [Fact]
    public async Task InitializeIsIdempotent()
    {
        await _service.InitializeTracksAsync();
        _service.UpdateTracks([Track(1)]);

        await _service.InitializeTracksAsync();

        Assert.Single(await _service.GetAvailableTracksAsync());
    }

    private static MikuMondayTrackInfo Track(int id) =>
        new()
        {
            Id = id,
            Number = id,
            BaseTrackInfoId = Guid.NewGuid(),
            TrackName = $"трек {id}",
            Artist = "Miku",
            Url = $"https://example.com/{id}",
        };
}
