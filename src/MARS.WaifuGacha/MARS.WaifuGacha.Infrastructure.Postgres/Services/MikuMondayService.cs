using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Clients;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.WaifuGacha.Infrastructure.Postgres.Services;

/// <summary>
/// Бизнес-логика награды Miku Monday: недельная активация треков по
/// пользователям, выбор трека для стримера и список доступных треков.
/// Портировано из монолита (TwitchMikuMondayRewardService +
/// MikuMondayTracksService); данные треков живут в waifu-схеме и заполняются
/// миграцией SeedLegacyWaifu.
/// </summary>
public class MikuMondayService(
    IDbContextFactory<WaifuDbContext> factory,
    ILogger<MikuMondayService> logger
)
{
    /// <summary>
    /// Случайный трек для пользователя с записью активации (один трек в неделю).
    /// </summary>
    public async Task<MikuMondayResult> GetRandomTrackForUserAsync(
        string twitchUserId,
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        var result = new MikuMondayResult();

        if (string.IsNullOrWhiteSpace(twitchUserId))
        {
            result.Error = "Не указан пользователь";
            return result;
        }

        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);

            var weekInfo = GetCurrentWeekOfYear();

            var existing = await db
                .MikuMondayActivations.AsNoTracking()
                .FirstOrDefaultAsync(
                    a =>
                        a.TwitchUserId == twitchUserId
                        && a.Year == weekInfo.Year
                        && a.WeekOfYear == weekInfo.WeekOfYear,
                    cancellationToken
                );

            if (existing is not null)
            {
                result.Error = "Вы уже активировали Miku Monday в этот понедельник! 🎤";
                return result;
            }

            var allTracks = await db
                .MikuMondayTracks.AsNoTracking()
                .OrderBy(t => t.Number)
                .ToListAsync(cancellationToken);

            if (allTracks.Count == 0)
            {
                result.Error = "Треки Miku не найдены в базе данных";
                logger.LogError("{Error}", result.Error);
                return result;
            }

            var usedTrackIds = await db
                .MikuMondayActivations.AsNoTracking()
                .Where(a => a.Year == weekInfo.Year && a.WeekOfYear == weekInfo.WeekOfYear)
                .Select(a => a.MikuMondayTrackId)
                .ToListAsync(cancellationToken);

            var available = allTracks.Where(t => !usedTrackIds.Contains(t.Id)).ToList();

            if (available.Count == 0)
            {
                result.Error =
                    "Все треки уже разобраны в этот понедельник! 🎵 Попробуйте в следующий понедельник!";
                return result;
            }

            var random = new Random();
            var selected = available[random.Next(available.Count)];

            var activation = new MikuMondayActivation
            {
                TwitchUserId = twitchUserId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? twitchUserId : displayName,
                MikuMondayTrackId = selected.Id,
                Year = weekInfo.Year,
                WeekOfYear = weekInfo.WeekOfYear,
            };

            db.MikuMondayActivations.Add(activation);
            await db.SaveChangesAsync(cancellationToken);

            result.Track = ToDto(selected);
            result.AvailableTracks = available
                .Where(t => t.Id != selected.Id)
                .Select(ToDto)
                .ToList();

            logger.LogInformation(
                "Пользователь {DisplayName} получил трек #{Number}: {TrackName}",
                displayName,
                selected.Number,
                selected.TrackName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при получении трека Miku Monday для {UserId}",
                twitchUserId
            );
            result.Error = "Произошла ошибка при получении трека";
        }

        return result;
    }

    /// <summary>
    /// Случайный трек для стримера без списания из очереди.
    /// </summary>
    public async Task<MikuMondayResult> GetRandomTrackForStreamerAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = new MikuMondayResult();

        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);

            var allTracks = await db
                .MikuMondayTracks.AsNoTracking()
                .OrderBy(t => t.Number)
                .ToListAsync(cancellationToken);

            if (allTracks.Count == 0)
            {
                result.Error = "Треки Miku не найдены в базе данных";
                logger.LogError("{Error}", result.Error);
                return result;
            }

            var weekInfo = GetCurrentWeekOfYear();
            var usedTrackIds = await db
                .MikuMondayActivations.AsNoTracking()
                .Where(a => a.Year == weekInfo.Year && a.WeekOfYear == weekInfo.WeekOfYear)
                .Select(a => a.MikuMondayTrackId)
                .ToListAsync(cancellationToken);

            var preferred = allTracks.Where(t => !usedTrackIds.Contains(t.Id)).ToList();
            var pool = preferred.Count > 0 ? preferred : allTracks;

            var random = new Random();
            var selected = pool[random.Next(pool.Count)];

            result.Track = ToDto(selected);
            result.AvailableTracks = (
                preferred.Count > 0
                    ? preferred.Where(t => t.Id != selected.Id)
                    : allTracks.Where(t => t.Id != selected.Id)
            )
                .Select(ToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении трека Miku Monday для стримера");
            result.Error = "Произошла ошибка при получении трека";
        }

        return result;
    }

    /// <summary>
    /// Список доступных треков для текущей недели.
    /// </summary>
    public async Task<List<MikuTrackDto>> GetAvailableTracksAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = new List<MikuTrackDto>();

        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);

            var weekInfo = GetCurrentWeekOfYear();

            var allTracks = await db
                .MikuMondayTracks.AsNoTracking()
                .OrderBy(t => t.Number)
                .ToListAsync(cancellationToken);

            var usedTrackIds = await db
                .MikuMondayActivations.AsNoTracking()
                .Where(a => a.Year == weekInfo.Year && a.WeekOfYear == weekInfo.WeekOfYear)
                .Select(a => a.MikuMondayTrackId)
                .ToListAsync(cancellationToken);

            result = allTracks.Where(t => !usedTrackIds.Contains(t.Id)).Select(ToDto).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка доступных треков Miku Monday");
        }

        return result;
    }

    private static MikuTrackDto ToDto(MikuMondayTrack track) =>
        new(
            track.BaseTrackInfoId,
            track.Number,
            track.Artist,
            track.TrackName,
            track.Url,
            track.ArtworkUrl
        );

    private static (int Year, int WeekOfYear) GetCurrentWeekOfYear()
    {
        var now = DateTime.Now;
        var calendar = CultureInfo.CurrentCulture.Calendar;
        return (now.Year, calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday));
    }
}
