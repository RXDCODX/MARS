using System.Globalization;
using MARS.Alerts.Extensions;
using MARS.Alerts.Models;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Заготовка награды MikuMonday, оставшаяся после переноса из монолита.
/// </summary>
/// <remarks>
/// <para>
/// Сервис ничем не вызывается: в <c>MARS.Alerts</c> нет ни потребителя треков,
/// ни консьюмера, который их приносит. Источник — <c>TwitchMikuMondayRewardService</c>
/// — при переносе не переехал (помечен как незавершённый в
/// <c>MIGRATION_CHECKLIST.md</c>), поэтому и <c>UpdateTracks</c> никто не зовёт.
/// </para>
/// <para>
/// Из-за этого <c>_tracks</c> в бою всегда пуст, а <c>GetRandomTrackForUserAsync</c>
/// всегда возвращает ошибку. Раньше это маскировалось записью в журнал про
/// «треки загружены через RabbitMQ» — такого ключа в <c>RabbitMqConfig</c> нет,
/// и пути, который эту запись оправдывал, тоже нет.
/// </para>
/// <para>
/// Класс оставлен намеренно, чтобы не выбрасывать заготовку незавершённой
/// фичи вместе с её тестами. Подключать его надо вместе с переносом источника
/// треков, иначе получится тот же мёртвый код с другой надписью.
/// </para>
/// </remarks>
public class MikuMondayTracksService(ILogger<MikuMondayTracksService> logger)
{
    private static readonly Lock Lock = new();
    private static bool _isInitialized = false;
    private readonly List<MikuMondayTrackInfo> _tracks = [];

    public Task InitializeTracksAsync()
    {
        lock (Lock)
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }
            _isInitialized = true;
        }

        // Честная формулировка вместо прежней: треки не загружены ниоткуда, и
        // сервис зарегистрирован, но не вызывается.
        logger.LogInformation(
            "MikuMondayTracksService создан, но треки не загружены: источник не перенесён из монолита, сервис в бою не вызывается"
        );
        return Task.CompletedTask;
    }

    public Task<MikuMondayResult> GetRandomTrackForUserAsync(
        string twitchUserId,
        string displayName
    )
    {
        var result = new MikuMondayResult();

        lock (Lock)
        {
            if (_tracks.Count == 0)
            {
                result.Error = "Треки Miku не загружены";
                return Task.FromResult(result);
            }

            var random = new Random();
            result.Track = _tracks[random.Next(_tracks.Count)];
            result.AvailableTracks = _tracks.Where(t => t.Id != result.Track.Id).ToList();
        }

        return Task.FromResult(result);
    }

    public Task<MikuMondayResult> GetRandomTrackForStreamerAsync()
    {
        return GetRandomTrackForUserAsync("streamer", "Streamer");
    }

    public Task<List<MikuMondayTrackInfo>> GetAvailableTracksAsync()
    {
        lock (Lock)
        {
            return Task.FromResult(_tracks.ToList());
        }
    }

    public void UpdateTracks(List<MikuMondayTrackInfo> tracks)
    {
        lock (Lock)
        {
            _tracks.Clear();
            _tracks.AddRange(tracks);
        }

        logger.LogInformation("Updated MikuMonday tracks: {Count} tracks loaded", tracks.Count);
    }
}
