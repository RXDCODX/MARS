using System.Globalization;
using MARS.Alerts.Extensions;
using MARS.Alerts.Models;

namespace MARS.Alerts.Services.Twitch.Rewards;

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

        logger.LogInformation(
            "MikuMondayTracksService initialized (stateless mode, tracks loaded via RabbitMQ)"
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

    private static WeekInfo GetCurrentWeekOfYear()
    {
        var result = new WeekInfo();
        var now = DateTime.Now;
        var calendar = CultureInfo.CurrentCulture.Calendar;

        result.Year = now.Year;
        result.WeekOfYear = calendar.GetWeekOfYear(
            now,
            CalendarWeekRule.FirstDay,
            DayOfWeek.Monday
        );

        return result;
    }

    private class WeekInfo
    {
        public int Year { get; set; }
        public int WeekOfYear { get; set; }
    }
}
